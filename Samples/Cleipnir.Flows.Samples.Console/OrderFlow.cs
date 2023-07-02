using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Console;

public class OrderFlow : Flow<string, RScrapbook>
{
    public override async Task Run(string orderId)
    {
        System.Console.WriteLine("Processing order: " + orderId);
        await Task.CompletedTask;
    }
}