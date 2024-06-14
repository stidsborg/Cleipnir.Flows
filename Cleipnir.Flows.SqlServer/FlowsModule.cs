using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.SqlServer;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.SqlServer;

public static class FlowsModule
{
    public static FlowsConfigurator UsePostgresSqlStore(
        this FlowsConfigurator configurator, string connectionString, bool initializeDatabase = true
    )
    {
        configurator.Services.AddSingleton<IFunctionStore>(
            _ =>
            {
                var store = new SqlServerFunctionStore(connectionString, tablePrefix: "flows");
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }
}