using Cleipnir.Flows.Persistence;
using Cleipnir.Flows.Sample.Console.AtMostOnce;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.Console.WaitForMessages;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<WaitForMessagesFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFlowStore(),
            serviceCollection.BuildServiceProvider()
        );

        var flows = new WaitForMessagesFlows(flowsContainer);
        var orderId = "MK-54321";
        await flows.Schedule(orderId, orderId);

        await Task.Delay(2_000);
        System.Console.WriteLine("Emitting: " + nameof(FundsReserved));
        var eventSourceWriter = flows.EventSourceWriter(orderId);
        await eventSourceWriter.AppendEvent(new FundsReserved(orderId), idempotencyKey: nameof(FundsReserved));

        await Task.Delay(2_000);
        System.Console.WriteLine("Emitting: " + nameof(InventoryLocked));
        await eventSourceWriter.AppendEvent(new InventoryLocked(orderId), idempotencyKey: nameof(InventoryLocked));

        var controlPanel = await flows.ControlPanel(orderId);
        await controlPanel!.WaitForCompletion();
        System.Console.WriteLine("Flow completed");
    }
}