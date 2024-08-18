using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using MassTransit;

namespace Cleipnir.Flows.MassTransit.Tests;

public class MassTransitTestFlows : Flows<MassTransitTestFlow>
{
    public MassTransitTestFlows(FlowsContainer flowsContainer) : base(flowName: "MassTransitTestFlow", flowsContainer) { }
}

public class MassTransitTestFlow : Flow
{
    public static volatile MyMessage? ReceivedMyMessage; 
    public override async Task Run() => ReceivedMyMessage = await Messages.FirstOfType<MyMessage>();
}

public class MassTransitTestFlowHandler(MassTransitTestFlows flows) : IConsumer<MyMessage>
{
    public Task Consume(ConsumeContext<MyMessage> context)
        => flows.SendMessage(context.Message.Value, context.Message);
}