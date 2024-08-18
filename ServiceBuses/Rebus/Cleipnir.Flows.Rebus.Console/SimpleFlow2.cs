using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Rebus.Console;

public class SimpleFlow2 : Flow
{
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow2({msg}) executed");
    }
}