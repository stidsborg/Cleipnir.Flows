namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.OutboxRip;

public class OutboxFlow : Flow<Order>
{
    public override async Task Run(Order order)
    {
        await StoreOrder(order);
        await PublishEvent(new OrderProcessed(order.OrderId));
    }

    #region Methods
    private Task StoreOrder(Order order) => Task.CompletedTask;
    private Task PublishEvent(object @event) => Task.CompletedTask;
    private record OrderProcessed(string OrderId);
    #endregion
}