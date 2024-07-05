using System;
using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.SqlServer;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.SqlServer;

public static class FlowsModule
{
    public static FlowsConfigurator UseSqlServerStore(
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
                var store = new SqlServerFunctionStore(connectionString, tablePrefix);
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }

    public static FlowsConfigurator UseSqlServerStore(
        this FlowsConfigurator configurator,
        string connectionString,
        bool initializeDatabase = true,
        string tablePrefix = "flows"
    ) => UseSqlServerStore(configurator, _ => connectionString, initializeDatabase, tablePrefix);
}