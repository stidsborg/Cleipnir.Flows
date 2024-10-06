using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.ConsoleApp.WaitForMessages;

[GenerateFlows]
public class WaitForMessagesFlow : Flow<string>
{
    public override async Task Run(string orderId)
    {
        await Messages
            .OfTypes<FundsReserved, InventoryLocked>()
            .Take(2)
            .ToList();

        System.Console.WriteLine("Complete order-processing");
    }
}