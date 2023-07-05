using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive;

namespace Cleipnir.Flows.Sample.Console;

public class OrderFlow : Flow<string, RScrapbook>
{
    public override async Task Run(string orderId)
    {
        System.Console.WriteLine("Processing order: " + orderId);
        await DoAtMostOnce("test", () => Task.FromResult("hello"));
        await Task.CompletedTask;
    }
}