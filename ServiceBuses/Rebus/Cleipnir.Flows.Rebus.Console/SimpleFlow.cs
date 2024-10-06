using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Rebus.Handlers;

namespace Cleipnir.Flows.Rebus.Console;

[GenerateFlows]
public class SimpleFlow : Flow
{
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow({msg}) executed");
    }
}

public class SimpleFlowsHandler(SimpleFlows simpleFlows) : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage msg) => simpleFlows.SendMessage(msg.Value, msg);
}