using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Cleipnir.Flows.AspNet;
using MassTransit;
using MassTransit.Configuration;

namespace Cleipnir.Flows.MassTransit;

public class CleipnirMassTransitConfiguation
{
    internal Dictionary<Type, Type> FlowsTypes { get; } = new();

    public CleipnirMassTransitConfiguation AddFlow<TFlows>() where TFlows : IBaseFlows
    {
        FlowsTypes[typeof(TFlows)] = TFlows.FlowType;
        return this;
    }

    public CleipnirMassTransitConfiguation AddFlowsAutomatically(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var assemblyFlowsTypes = assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i == typeof(IBaseFlows)))
            .Where(t => !t.IsAbstract);

        foreach (var assemblyFlowsType in assemblyFlowsTypes)
        {
            var baseType = assemblyFlowsType.BaseType;
            Type? flowType = null; 
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(BaseFlows<>))
                    flowType = baseType.GenericTypeArguments[0];
                
                baseType = baseType.BaseType;
            }

            if (flowType != null)
                FlowsTypes.TryAdd(assemblyFlowsType, flowType);
        }
        
        return this;
    }
}

public static class MassTransitExtensions
{
    public static FlowsConfigurator IntegrateWithMassTransit(
        this FlowsConfigurator flowsConfigurator, 
        Func<CleipnirMassTransitConfiguation, CleipnirMassTransitConfiguation>? config = null)
    {
        var configuration = new CleipnirMassTransitConfiguation();
        if (config != null)
            config(configuration);
        else
            configuration.AddFlowsAutomatically(
                Assembly.GetCallingAssembly()
            );

        var massTransitConsumerTypes = CreateConsumerTypes(configuration.FlowsTypes);
        foreach (var consumerType in massTransitConsumerTypes)
        {
            flowsConfigurator.Services.RegisterConsumer(
                new DependencyInjectionContainerRegistrar(flowsConfigurator.Services),
                consumerType
            );
        }
        
        return flowsConfigurator;
    }
    
    private static IEnumerable<Type> CreateConsumerTypes(Dictionary<Type, Type> flowsDictionary)
    {
        var assemblyName = new AssemblyName("CleipnirMassTransitIntegration");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run
        );
        var module = assembly.DefineDynamicModule("CleipnirMassTransitModule");

        var integrationHandlerTypes = new List<Type>(flowsDictionary.Count);
        
        foreach (var (flowsType, flowType) in flowsDictionary)
        {
            var baseHandlerType = typeof(MassTransitGenericHandler<,>).MakeGenericType(flowsType, flowType);
            //todo make this type specific name to avoid collisions
            var type = module.DefineType($"{flowType.Name}Consumer", TypeAttributes.Public, parent: baseHandlerType);

            // Define Constructor
            var ctor = type.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                [flowsType]
            );
            var ilGenerator = ctor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0); //loads this onto the stack
            ilGenerator.Emit(OpCodes.Ldarg_1); //loads Flows input parameter onto the stack
            ilGenerator.Emit(OpCodes.Call,
                baseHandlerType.GetConstructor([flowsType])!); // Call base constructor (object)
            ilGenerator.Emit(OpCodes.Ret); // Return

            //Implement handler types
            var handlerTypes = flowType
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISubscription<>))
                .Select(t => t.GenericTypeArguments[0])
                .Select(t => 
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ConsumeContext<>)
                        ? t.GenericTypeArguments[0]
                        : t
                    )
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                type.AddInterfaceImplementation(typeof(IConsumer<>).MakeGenericType(handlerType));
                var consumeContextHandlerType = typeof(ConsumeContext<>).MakeGenericType(handlerType);
                var handleMethod = type.DefineMethod(
                    "Consume",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    returnType: typeof(Task),
                    [consumeContextHandlerType]
                );

                var handleIl = handleMethod.GetILGenerator();
                handleIl.Emit(OpCodes.Ldarg_0); // Load "this" onto the stack
                handleIl.Emit(OpCodes.Ldarg_1); // Load message input parameter onto the stack

                var handleIncomingMessageMethod =
                    baseHandlerType.GetMethods().Single(m => m.Name == "HandleIncomingMessage");
                var genericHandleIncomingMessageMethod = handleIncomingMessageMethod.MakeGenericMethod(handlerType);
                handleIl.Emit(OpCodes.Callvirt,
                    genericHandleIncomingMessageMethod); // Invoke flowsContainer.DeliverMessage

                handleIl.Emit(OpCodes.Ret); // Return task from previous call from the method        
            }

            var rebusMessageHandlerType = type.CreateType();
            integrationHandlerTypes.Add(rebusMessageHandlerType);
        }

        return integrationHandlerTypes;
    }
}