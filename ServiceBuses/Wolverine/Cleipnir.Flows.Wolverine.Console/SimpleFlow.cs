using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Wolverine.Console;

[SourceGeneration.Ignore]
public class SimpleFlow : Flow
{
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow({msg}) executed");
    }
}

public class SimpleFlows : Flows<SimpleFlow>
{
    public SimpleFlows(FlowsContainer flowsContainer) 
        : base("SimpleFlow", flowsContainer, options: null) { }
}

public class SimpleFlowsHandler(SimpleFlows flows)
{
    public Task Handle(MyMessage myMessage)
        => flows.SendMessage(myMessage.Value, myMessage);
}