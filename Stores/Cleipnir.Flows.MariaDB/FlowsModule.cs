using System;
using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.MariaDb;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.MariaDB;

public static class FlowsModule
{
    public static FlowsConfigurator UseMariaSqlStore(
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
                var store = new MariaDbFunctionStore(connectionString, tablePrefix);
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }

    public static FlowsConfigurator UseMariaSqlStore(
        this FlowsConfigurator configurator,
        string connectionString,
        bool initializeDatabase = true,
        string tablePrefix = "flows"
    ) => UseMariaSqlStore(configurator, _ => connectionString, initializeDatabase, tablePrefix);
}