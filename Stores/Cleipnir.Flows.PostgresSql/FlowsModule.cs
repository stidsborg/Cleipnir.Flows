using System;
using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.PostgreSQL;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.PostgresSql;

public static class FlowsModule
{
    public static FlowsConfigurator UsePostgresStore(
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
                var store = new PostgreSqlFunctionStore(connectionString, tablePrefix);
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }

    public static FlowsConfigurator UsePostgresStore(
        this FlowsConfigurator configurator,
        string connectionString,
        bool initializeDatabase = true,
        string tablePrefix = "flows"
    ) => UsePostgresStore(configurator, _ => connectionString, initializeDatabase, tablePrefix);
}