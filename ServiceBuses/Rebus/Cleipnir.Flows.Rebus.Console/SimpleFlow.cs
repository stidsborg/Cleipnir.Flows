using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Rebus.Console;

public class SimpleFlow : Flow
{
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow({msg}) executed");
    }
}