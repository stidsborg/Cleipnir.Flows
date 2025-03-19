using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;

namespace Cleipnir.Flows.Tests.AspNet;

[TestClass]
public class BulkPublishTests
{
    [TestMethod]
    public Task MultipleMessageCanBePublishedUsingInMemoryStore()
        => MultipleMessageCanBePublished(Task.FromResult((IFunctionStore)new InMemoryFunctionStore()));
    
    [TestMethod]
    public Task MultipleMessageCanBePublishedUsingPostgres()
        => MultipleMessageCanBePublished(PostgresSqlHelper.CreateAndInitializeStore());
    
    [TestMethod]
    public Task MultipleMessageCanBePublishedUsingMariaDb()
        => MultipleMessageCanBePublished(MariaDbHelper.CreateAndInitializeMySqlStore());
    
    [TestMethod]
    public Task MultipleMessageCanBePublishedUsingSqlServer()
        => MultipleMessageCanBePublished(SqlServerHelper.CreateAndInitializeStore());

    private async Task MultipleMessageCanBePublished(Task<IFunctionStore> storeTask)
    {
        using var container = FlowsContainer.Create(functionStore: await storeTask);
        var flows = container.RegisterAnonymousFlow(() => new TestFlow(), flowName: $"TestFlow#{Guid.NewGuid()}");

        var instances = Enumerable.Range(0, 100)
            .Select(i => i.ToString().ToFlowInstance())
            .ToList();
        await flows.BulkSchedule(instances);
        
        await flows.SendMessages(
            instances.Select(instance => new BatchedMessage(
                Instance: instance,
                Message: $"Hello {instance}"
            )).ToList()
        );

        for (var i = 0; i < 100; i++)
        {
            var controlPanel = await flows.ControlPanel(i.ToString());
            await controlPanel!.WaitForCompletion(maxWait: TimeSpan.FromSeconds(10), allowPostponeAndSuspended: true);
        }
    }

    private class TestFlow : Flow
    {
        public override async Task Run()
        {
            await Messages.FirstOfType<string>();
        }
    }
}