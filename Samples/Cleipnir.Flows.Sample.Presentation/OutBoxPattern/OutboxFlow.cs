using Cleipnir.Flows.Sample.Presentation.A_OrderFlowRpc;

namespace Cleipnir.Flows.Sample.Presentation.OutBoxPattern;

public class OutboxFlow(IBus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        _ = bus; //fix compiler warning
        
        await PersistOrder(order);
        await PersistToOutboxTable(new OrderHandled(order.OrderId));
    }

    #region SaveOrderStateToDatabase

    private Task PersistOrder(Order order) => Task.CompletedTask;
    private Task PersistToOutboxTable(object message) => Task.CompletedTask;

    private record OrderHandled(string OrderNumber);

    #endregion
}