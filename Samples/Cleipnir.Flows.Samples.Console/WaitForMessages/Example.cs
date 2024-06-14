using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.ConsoleApp.WaitForMessages;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<WaitForMessagesFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new WaitForMessagesFlows(flowsContainer);
        var orderId = "MK-54321";
        await flows.Schedule(orderId, orderId);

        await Task.Delay(2_000);
        System.Console.WriteLine("Emitting: " + nameof(FundsReserved));
        var messageWriter = flows.MessageWriter(orderId);
        await messageWriter.AppendMessage(new FundsReserved(orderId), idempotencyKey: nameof(FundsReserved));

        await Task.Delay(2_000);
        System.Console.WriteLine("Emitting: " + nameof(InventoryLocked));
        await messageWriter.AppendMessage(new InventoryLocked(orderId), idempotencyKey: nameof(InventoryLocked));

        var controlPanel = await flows.ControlPanel(orderId);
        await controlPanel!.WaitForCompletion();
        System.Console.WriteLine("Flow completed");
    }
}