using Rebus.Handlers;

namespace Cleipnir.Flows.Rebus.Console;

public class SimpleFlowsHandler(SimpleFlows simpleFlows) : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage msg) => simpleFlows.SendMessage(msg.Value, msg);
}