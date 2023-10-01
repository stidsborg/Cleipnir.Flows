using Cleipnir.Flows.Reactive;
using Cleipnir.ResilientFunctions.Domain;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging;

public class OrderFlow : Flow<Order, OrderFlow.OrderScrapbook>
{
    private readonly MessageBroker _messageBroker;
    private ILogger Logger { get; } = Log.Logger.ForContext<OrderFlow>();

    public OrderFlow(MessageBroker messageBroker) => _messageBroker = messageBroker;

    public override async Task Run(Order order)
    {
        Logger.Information($"Processing of order '{order.OrderId}' started");
 
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

    public class OrderScrapbook : RScrapbook
    {
        
    }
}
