using System;
using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.MySQL;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.MySQL;

public static class FlowsModule
{
    public static FlowsConfigurator UseMySqlStore(
        this FlowsConfigurator configurator, 
        Func<IServiceProvider, string> connectionStringFunc, 
        bool initializeDatabase = true,
        string tablePrefix = "flows"
    )
    {
        configurator.Services.AddSingleton<IFunctionStore>(
            sp =>
            {
                var connectionString = connectionStringFunc(sp);
                var store = new MySqlFunctionStore(connectionString, tablePrefix);
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }

    public static FlowsConfigurator UseMySqlStore(
        this FlowsConfigurator configurator,
        string connectionString,
        bool initializeDatabase = true,
        string tablePrefix = "flows"
    ) => UseMySqlStore(configurator, _ => connectionString, initializeDatabase, tablePrefix);
}