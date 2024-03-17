using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging.Solution;

public class OrderFlow(MessageBroker messageBroker) : Flow<Order>
{
    private ILogger Logger { get; } = Log.Logger.ForContext<OrderFlow>();

    public override async Task Run(Order order)
    {
        Logger.Information($"Processing of order '{order.OrderId}' started");

        var transactionId = await Effect.CreateOrGet("TransactionId", Guid.NewGuid());
        
        await messageBroker.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId));
        await Messages.FirstOfType<FundsReserved>();

        await messageBroker.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));

        await Messages.FirstOfType<ProductsShipped>();
        
        await messageBroker.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId));
        await Messages.FirstOfType<FundsCaptured>();

        await messageBroker.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
        await Messages.FirstOfType<OrderConfirmationEmailSent>();

        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }
    
    /*
       var either = await Messages.OfTypes<ProductsShipped, ProductsShipmentFailed>().First();
       if (either.HasSecond)
       {
           await _messageBroker.Send(new CancelFundsReservation(order.OrderId, transactionId));
           return;
       }
     */
}
