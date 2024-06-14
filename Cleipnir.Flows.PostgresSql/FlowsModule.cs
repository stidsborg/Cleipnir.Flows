using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.PostgreSQL;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.PostgresSql;

public static class FlowsModule
{
    public static FlowsConfigurator UsePostgresSqlStore(
        this FlowsConfigurator configurator, string connectionString, bool initializeDatabase = true
    )
    {
        configurator.Services.AddSingleton<IFunctionStore>(
            _ =>
            {
                var store = new PostgreSqlFunctionStore(connectionString, tablePrefix: "flows");
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }
}