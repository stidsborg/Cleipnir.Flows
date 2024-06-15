using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cleipnir.Flows.SourceGeneration;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.AspNet;

public static class FlowsModule
{
    public static IServiceCollection UseFlows(this IServiceCollection services, Func<FlowsConfigurator, FlowsConfigurator> configure)
    {
        var configurator = new FlowsConfigurator(services);
        configure(configurator);
        
        services.AddSingleton(configurator.Options);
        services.AddSingleton<FlowsContainer>();

        services.AddHostedService(
            s => new FlowsHostedService(s, configurator.FlowsTypes, configurator.EnableGracefulShutdown)
        );
        return services;
    }
}

public class FlowsConfigurator
{
    internal Options Options = new();
    internal bool EnableGracefulShutdown = false;
    internal IEnumerable<Type> FlowsTypes = [];
    public IServiceCollection Services { get; }

    public FlowsConfigurator(IServiceCollection services)
    {
        Services = services;
    }

    public FlowsConfigurator UseInMemoryStore()
    {
        Services.AddSingleton<IFunctionStore>(new InMemoryFunctionStore());
        return this;
    }
    
    public FlowsConfigurator UseStore(IFunctionStore store)
    {
        Services.AddSingleton(store);
        return this;
    }

    public FlowsConfigurator WithOptions(Options options)
    {
        Options = options;
        return this;
    }

    public FlowsConfigurator RegisterFlow<TFlow, TFlows>() where TFlow : Flow where TFlows : BaseFlows<TFlow>
    {
        Services.AddScoped<TFlow>();
        Services.AddTransient<TFlows>();
        FlowsTypes = FlowsTypes.Append(typeof(TFlows));

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
            Services.AddTransient(sourceGeneratedFlowsType);
            var flowType = sourceGeneratedFlowsType.BaseType?.GenericTypeArguments[0];
            if (flowType != null)
                Services.AddScoped(flowType);
            
            FlowsTypes = FlowsTypes.Append(sourceGeneratedFlowsType);
        }

        return this;
    }

    public FlowsConfigurator GracefulShutdown(bool enable)
    {
        EnableGracefulShutdown = enable;
        return this;
    }
}