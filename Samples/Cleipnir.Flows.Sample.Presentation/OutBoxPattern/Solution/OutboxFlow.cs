using Cleipnir.Flows.Sample.Presentation.A_OrderFlowRpc;

namespace Cleipnir.Flows.Sample.Presentation.OutBoxPattern.Solution;

[GenerateFlows]
public class OutboxFlow(IBus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        await Capture(() => PersistOrder(order));
        await Capture(() => bus.Publish(new OrderHandled(order.OrderId)));
    }

    #region SaveOrderStateToDatabase
    private Task PersistOrder(Order order) => Task.CompletedTask;
    private record OrderHandled(string OrderNumber);

    #endregion
}