using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Cleipnir.Flows.AspNet;
using Rebus.Config;
using Rebus.Handlers;

namespace Cleipnir.Flows.Rebus;

public class CleipnirRebusConfiguation
{
    internal Dictionary<Type, Type> FlowsTypes { get; } = new();

    public CleipnirRebusConfiguation AddFlow<TFlows>() where TFlows : IBaseFlows
    {
        FlowsTypes[typeof(TFlows)] = TFlows.FlowType;
        return this;
    }

    public CleipnirRebusConfiguation AddFlowsAutomatically(Assembly? assembly = null)
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

public static class RebusExtensions
{
    public static FlowsConfigurator IntegrateWithRebus(
        this FlowsConfigurator flowsConfigurator, 
        Func<CleipnirRebusConfiguation, CleipnirRebusConfiguation>? config = null)
    {
        var configuration = new CleipnirRebusConfiguation();
        if (config != null)
            config(configuration);
        else
            configuration.AddFlowsAutomatically(
                Assembly.GetCallingAssembly()
            );

        var rebusHandlerTypes = CreateHandlerTypes(configuration.FlowsTypes);
        foreach (var rebusHandlerType in rebusHandlerTypes)
            flowsConfigurator.Services.AddRebusHandler(rebusHandlerType);   
        
        return flowsConfigurator;
    }
    
    private static IEnumerable<Type> CreateHandlerTypes(Dictionary<Type, Type> flowsDictionary)
    {
        /*
         Handler high-level:
         
         public class CleipnirRebusHandler : IHandleMessages<T1>, IHandleMessages<T2> ... 
         {
            public FlowsContainer _flowsContainer;
            public CleipnirRebusHandler(FlowsContainer flowsContainer) => _flowsContainer = flowsContainer;
         
            public Task Handle(T1 msg) => _flowsContainer.DeliverMessage(msg);
            public Task Handle(T2 msg) => _flowsContainer.DeliverMessage(msg);
            ...        
         }
         */
        
        var assemblyName = new AssemblyName("CleipnirRebusIntegration");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run
        );
        var module = assembly.DefineDynamicModule("CleipnirRebusModule");

        var rebusIntegrationHandlerTypes = new List<Type>(flowsDictionary.Count);
        
        foreach (var (flowsType, flowType) in flowsDictionary)
        {
            var baseHandlerType = typeof(RebusGenericHandler<>).MakeGenericType(flowsType);
            var type = module.DefineType($"{flowType.Name}Handler", TypeAttributes.Public, parent: baseHandlerType);

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
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                type.AddInterfaceImplementation(typeof(IHandleMessages<>).MakeGenericType(handlerType));
                var handleMethod = type.DefineMethod(
                    "Handle",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    returnType: typeof(Task),
                    [handlerType]
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
            rebusIntegrationHandlerTypes.Add(rebusMessageHandlerType);
        }

        return rebusIntegrationHandlerTypes;
    }
}