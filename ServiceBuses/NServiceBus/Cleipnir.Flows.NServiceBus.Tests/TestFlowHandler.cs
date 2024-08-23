namespace Cleipnir.Flows.NServiceBus.Tests;

public class TestFlowHandler(IntegrationTests.TestFlows flows)  : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage message, IMessageHandlerContext context) 
        => flows.SendMessage(message.Value, message);
}