using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.MySQL;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.MySQL;

public static class FlowsModule
{
    public static FlowsConfigurator UsePostgresSqlStore(
        this FlowsConfigurator configurator, string connectionString, bool initializeDatabase = true
    )
    {
        configurator.Services.AddSingleton<IFunctionStore>(
            _ =>
            {
                var store = new MySqlFunctionStore(connectionString, tablePrefix: "flows");
                if (initializeDatabase)
                    store.Initialize().GetAwaiter().GetResult();

                return store;
            });

        return configurator;
    }
}