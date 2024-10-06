using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.B_OrderFlow_Messaging;

[GenerateFlows]
public class OrderFlow : Flow<Order>
{
    /*
     * 1. In-memory execution
     * 2. Suspend execution while waiting for messages
     */
    
    private Bus Bus { get; }

    public OrderFlow(Bus bus) => Bus = bus;
    
    public override async Task Run(Order order)
    {
        var transactionId = await Effect.CreateOrGet("TransactionId", Guid.NewGuid());

        await Bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId));
        await Messages.FirstOfType<FundsReserved>();

        await Bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
        await Messages.FirstOfType<ProductsShipped>();
        
        await Bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId));
        await Messages.FirstOfType<FundsCaptured>();

        await Bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
        await Messages.FirstOfType<OrderConfirmationEmailSent>();
    }
}
