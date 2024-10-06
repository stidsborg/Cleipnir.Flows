using Cleipnir.ResilientFunctions.PostgreSQL;

namespace Cleipnir.Flows.Sample.Presentation.H_BankTransfer;

public static class Example
{
    public static async Task Perform()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<TransferFlow>();
        
        var connStr = "Server=localhost;Database=presentation;User Id=postgres;Password=Pa55word!; Include Error Detail=true;";
        await DatabaseHelper.CreateDatabaseIfNotExists(connStr);
        var store = new PostgreSqlFunctionStore(connStr);
        await store.Initialize();
        
        var flowsContainer = new FlowsContainer(
            store,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var transferFlows = new TransferFlows(flowsContainer);
        var transactionId = Guid.NewGuid();
        await transferFlows.Run(
            transactionId.ToString(), new Transfer(transactionId, "FROM_ACC123", "TO_ACC456", Amount: 100)
        );
    }
}