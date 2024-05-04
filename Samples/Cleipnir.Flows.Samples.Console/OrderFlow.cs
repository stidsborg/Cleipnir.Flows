namespace Cleipnir.Flows.Sample.ConsoleApp;

public class OrderFlow : Flow<string>
{
    public override async Task Run(string orderId)
    {
        System.Console.WriteLine("Processing order: " + orderId);
        await Effect.Capture(id: "test", () => Task.FromResult("hello"));
        await Task.CompletedTask;
    }
}