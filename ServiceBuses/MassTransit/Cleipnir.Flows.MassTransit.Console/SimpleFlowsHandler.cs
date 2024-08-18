using MassTransit;

namespace Cleipnir.Flows.MassTransit.Console;

public class SimpleFlowsHandler(SimpleFlows simpleFlows) : IConsumer<MyMessage>
{
    public Task Consume(ConsumeContext<MyMessage> context) 
        => simpleFlows.SendMessage(context.Message.Value, context.Message);
}