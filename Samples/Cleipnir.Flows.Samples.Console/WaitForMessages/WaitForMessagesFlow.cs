using Cleipnir.Flows.Reactive;

namespace Cleipnir.Flows.Sample.Console.WaitForMessages;

public class WaitForMessagesFlow : Flow<string>
{
    public override async Task Run(string orderId)
    {
        await EventSource
            .OfTypes<FundsReserved, InventoryLocked>()
            .Take(2)
            .ToList();

        System.Console.WriteLine("Complete order-processing");
    }
}