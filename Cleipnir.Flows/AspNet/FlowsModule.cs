using System;
using System.Linq;
using System.Reflection;
using Cleipnir.Flows.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.AspNet;

public static class FlowsModule
{
    public static IServiceCollection UseFlows(
        IServiceCollection services,
        IFlowStore flowStore,
        Func<IServiceProvider, Options>? options = null,
        bool gracefulShutdown = false,
        Assembly? rootAssembly = null,        
        bool initializeDatabase = true,
        bool automaticallyRegisterSourceGeneratedFlows = true
    )
    {      
        if (initializeDatabase)
            flowStore.Initialize().GetAwaiter().GetResult();

        if (options != null)
            services.AddSingleton(options);
        services.AddSingleton(flowStore);
        services.AddSingleton<FlowsContainer>();

        rootAssembly ??= Assembly.GetCallingAssembly();

        if (automaticallyRegisterSourceGeneratedFlows)
        {
            var sourceGeneratedFlowsTypes = rootAssembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] { rootAssembly })
                .SelectMany(a => a.GetTypes())
                .Where(IsSourceGeneratedFlowsType);

            foreach (var sourceGeneratedFlowsType in sourceGeneratedFlowsTypes)
                services.AddTransient(sourceGeneratedFlowsType);
        }

        services.AddHostedService(s => new FlowsHostedService(s, rootAssembly, gracefulShutdown));
        return services;
    }

    private static bool IsSourceGeneratedFlowsType(Type type) 
        => type.GetCustomAttribute<SourceGeneratedFlowsAttribute>() != null;
}