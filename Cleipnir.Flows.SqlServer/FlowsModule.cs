using System;
using System.Reflection;
using Cleipnir.ResilientFunctions.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.SqlServer;

public static class FlowsModule
{
    public static IServiceCollection UseFlows(
        this IServiceCollection services, 
        string connectionString,
        Func<IServiceProvider, Options>? options = null,
        bool gracefulShutdown = false,
        Assembly? rootAssembly = null,
        bool initializeDatabase = true
    )
    {
        var flowStore = new SqlServerFunctionStore(connectionString, tablePrefix: "Flows");
        
        return AspNet.FlowsModule.UseFlows(
            services,
            flowStore,
            options,
            gracefulShutdown,
            rootAssembly ?? Assembly.GetCallingAssembly(),
            initializeDatabase
        );
    }
}