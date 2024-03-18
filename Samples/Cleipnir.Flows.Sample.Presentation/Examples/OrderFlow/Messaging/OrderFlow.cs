using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging;

public class OrderFlow : Flow<Order>
{
    private MessageBroker MessageBroker { get; }
    private ILogger Logger { get; } = Log.Logger.ForContext<OrderFlow>();

    public OrderFlow(MessageBroker messageBroker) => MessageBroker = messageBroker;
    
    public override async Task Run(Order order)
    {
        Logger.Information($"Processing of order '{order.OrderId}' started");
        
        Logger.Information($"Processing of order '{order.OrderId}' started");

        var transactionId = await Effect.CreateOrGet("TransactionId", Guid.NewGuid());
        
        await MessageBroker.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId));
        //wait for ReserveFunds

        await MessageBroker.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
        //wait for ProductsShipped
        
        await MessageBroker.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId));
        //wait for FundsCaptured

        await MessageBroker.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
        //wait for order OrderConfirmationEmailSent

        Logger.Information($"Processing of order '{order.OrderId}' completed");
        
        /*
         Command types:
         - ReserveFunds
         - ShipProducts
         - CaptureFunds
         - SendOrderConfirmationEmail
         
         Event types:                  
         - FundsReserved         
         - ProductsShipped         
         - FundsCaptured         
         - OrderConfirmationEmailSent         
        */ 
     
        await Task.CompletedTask;
        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }
}
