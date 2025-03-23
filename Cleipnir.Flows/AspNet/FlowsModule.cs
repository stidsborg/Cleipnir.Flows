using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cleipnir.Flows.SourceGeneration;
using Cleipnir.ResilientFunctions.CoreRuntime;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.AspNet;

public static class FlowsModule
{
    public static IServiceCollection AddFlows(this IServiceCollection services, Func<FlowsConfigurator, FlowsConfigurator> configure)
    {
        var configurator = new FlowsConfigurator(services);
        configure(configurator);

        if (configurator.OptionsFunc is null)
            services.AddSingleton(new Options());
        else
            services.AddSingleton(configurator.OptionsFunc);
        
        services.AddSingleton<FlowsContainer>();
        services.AddTransient<Workflow>(_ => CurrentFlow.Workflow ?? throw new InvalidOperationException("Workflow is not present outside Flow"));
        services.AddTransient<Effect>(_ => CurrentFlow.Workflow?.Effect ?? throw new InvalidOperationException("Effect is not present outside Flow"));
        services.AddTransient<Messages>(_ => CurrentFlow.Workflow?.Messages ?? throw new InvalidOperationException("Messages is not present outside Flow"));
        
        services.AddHostedService(
            s => new FlowsHostedService(s, configurator.FlowsTypes, configurator.EnableGracefulShutdown)
        );
        return services;
    }
}

public class FlowsConfigurator
{
    internal bool EnableGracefulShutdown = false;
    internal readonly HashSet<Type> FlowsTypes = new();

    internal Func<IServiceProvider, Options>? OptionsFunc;
    public IServiceCollection Services { get; }

    public FlowsConfigurator(IServiceCollection services)
    {
        Services = services;
    }

    public FlowsConfigurator UseInMemoryStore(InMemoryFunctionStore? store = null)
    {
        Services.AddSingleton<IFunctionStore>(store ?? new InMemoryFunctionStore());
        return this;
    }
    
    public FlowsConfigurator UseStore(IFunctionStore store)
    {
        Services.AddSingleton(store);
        return this;
    }

    public FlowsConfigurator WithOptions(Options options)
        => WithOptions(_ => options);
    
    public FlowsConfigurator WithOptions(Func<IServiceProvider, Options> optionsFunc)
    {
        OptionsFunc = optionsFunc;
        return this;
    }

    public FlowsConfigurator RegisterFlow<TFlow, TFlows>() where TFlow : BaseFlow where TFlows : BaseFlows<TFlow>
    {
        var added = FlowsTypes.Add(typeof(TFlows));
        if (!added) return this;
        
        Services.AddScoped<TFlow>();
        Services.AddTransient<TFlows>();
        
        return this;
    }
    
    public FlowsConfigurator RegisterFlow<TFlow, TFlows>(Func<IServiceProvider, TFlows> flowsFactory, Func<IServiceProvider, TFlow>? flowFactory = null) where TFlow : BaseFlow where TFlows : BaseFlows<TFlow>
    {
        var added = FlowsTypes.Add(typeof(TFlows));
        if (!added) return this;

        if (flowFactory != null)
            Services.AddScoped(flowFactory);
        else
            Services.AddScoped<TFlow>();
        
        Services.AddTransient(flowsFactory);

        return this;
    }

    public FlowsConfigurator RegisterFlowsAutomatically(Assembly? rootAssembly = null)
    {
        bool IsSourceGeneratedFlowsType(Type type) 
            => type.GetCustomAttribute<SourceGeneratedFlowsAttribute>() != null;
        
        rootAssembly ??= Assembly.GetCallingAssembly();
        var sourceGeneratedFlowsTypes = rootAssembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Concat(new[] { rootAssembly })
            .SelectMany(a => a.GetTypes())
            .Where(IsSourceGeneratedFlowsType);

        foreach (var sourceGeneratedFlowsType in sourceGeneratedFlowsTypes)
        {
            var added = FlowsTypes.Add(sourceGeneratedFlowsType);
            if (!added) continue;
            
            Services.AddTransient(sourceGeneratedFlowsType);
            var flowType = sourceGeneratedFlowsType.BaseType?.GenericTypeArguments[0];
            if (flowType != null)
                Services.AddScoped(flowType);
        }

        return this;
    }

    public FlowsConfigurator GracefulShutdown(bool enable)
    {
        EnableGracefulShutdown = enable;
        return this;
    }
}