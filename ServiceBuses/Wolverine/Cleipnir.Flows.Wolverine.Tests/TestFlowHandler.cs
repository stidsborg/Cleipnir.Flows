namespace Cleipnir.Flows.Wolverine.Tests;

public class TestFlowHandler(IntegrationTests.TestFlows flows) 
{
    public Task Handle(IntegrationTests.MyMessage message) => flows.SendMessage(message.Value, message);
}