using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Handlers;

namespace Cleipnir.Flows.Rebus;

public class CleipnirRebusConfiguation
{
    internal HashSet<Type> FlowTypes { get; } = new();

    public CleipnirRebusConfiguation AddFlow<TFlow>() where TFlow : Flow
    {
        FlowTypes.Add(typeof(TFlow));
        return this;
    }

    public CleipnirRebusConfiguation AddFlowsAutomatically(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var assemblyFlowTypes = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseFlow)));

        foreach (var assemblyFlowType in assemblyFlowTypes)
            FlowTypes.Add(assemblyFlowType);
        
        return this;
    }
}

public static class RebusExtensions
{
    public static IServiceCollection IntegrateRebusWithFlows(
        this IServiceCollection services, 
        Func<CleipnirRebusConfiguation, CleipnirRebusConfiguation> config)
    {
        var configuration = new CleipnirRebusConfiguation();
        config(configuration);

        var messageTypes = configuration.FlowTypes
            .SelectMany(t => t.GetInterfaces())
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISubscribeTo<>))
            .Select(t => t.GenericTypeArguments[0])
            .Distinct()
            .ToList();
        
        var rebusHandlerType = CreateHandlerType(messageTypes);
        services.AddRebusHandler(rebusHandlerType);

        return services;
    }
    
    private static Type CreateHandlerType(IEnumerable<Type> handlerTypes)
    {
        /*
         Handler high-level:
         
         public class CleipnirRebusHandler : IHandleMessages<T1>, IHandleMessages<T2> ... 
         {
            public FlowsContainer _flowsContainer;
            public CleipnirRebusHandler(FlowsContainer flowsContainer) => _flowsContainer = flowsContainer;
         
            public Task Handle(T1 msg) => _flowsContainer(msg);
            public Task Handle(T2 msg) => _flowsContainer(msg);
            ...        
         }
         */
        
        var deliverMessageMethod = typeof(FlowsContainer).GetMethod(nameof(FlowsContainer.DeliverMessage));
        ArgumentNullException.ThrowIfNull(deliverMessageMethod);    
        
        var assemblyName = new AssemblyName("CleipnirRebusIntegration");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run
        );
        var module = assembly.DefineDynamicModule("CleipnirRebusModule");
        var type = module.DefineType("CleipnirRebusHandler", TypeAttributes.Public);
        
        var flowContainerField = type.DefineField(nameof(FlowsContainer), typeof(FlowsContainer), FieldAttributes.Public);
        
        // Define Constructor
        var ctor = type.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(FlowsContainer)]
        );
        var ilGenerator = ctor.GetILGenerator(); 
        ilGenerator.Emit(OpCodes.Ldarg_0); //loads this onto the stack
        ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!); // Call base constructor (object)
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load "this" onto the stack
        ilGenerator.Emit(OpCodes.Ldarg_1); // Load the first argument onto the stack
        ilGenerator.Emit(OpCodes.Stfld, flowContainerField); // Store the value in the field
        ilGenerator.Emit(OpCodes.Ret); // Return

        //Implement handler types
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
            handleIl.Emit(OpCodes.Ldfld, flowContainerField); // Load the flow-container field onto the stack (poping "this" in the process)
            handleIl.Emit(OpCodes.Ldarg_1); // Load message input parameter onto the stack
        
            handleIl.Emit(OpCodes.Callvirt, deliverMessageMethod.MakeGenericMethod(handlerType)); // Invoke flowsContainer.DeliverMessage
            handleIl.Emit(OpCodes.Ret); // Return task from previous call from the method        
        }
        var rebusMessageHandlerType = type.CreateType();
        return rebusMessageHandlerType;
    }
}