using System;
using System.Reflection;
using Cleipnir.ResilientFunctions.MySQL;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.MySQL;

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
        var flowStore = new MySqlFunctionStore(connectionString, tablePrefix: "flows");
        
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