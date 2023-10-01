using Cleipnir.Flows.Reactive;
using Cleipnir.ResilientFunctions.Domain;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging.Solution;

public class OrderFlow : Flow<Order, OrderFlow.OrderScrapbook>
{
    private readonly MessageBroker _messageBroker;
    private ILogger Logger { get; } = Log.Logger.ForContext<OrderFlow>();

    public OrderFlow(MessageBroker messageBroker) => _messageBroker = messageBroker;

    public override async Task Run(Order order)
    {
        Logger.Information($"Processing of order '{order.OrderId}' started");
 
        await _messageBroker.Send(new ReserveFunds(order.OrderId, order.TotalPrice, Scrapbook.TransactionId, order.CustomerId));
        await EventSource.NextOfType<FundsReserved>();

        await _messageBroker.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
        await EventSource.NextOfType<ProductsShipped>();

        await _messageBroker.Send(new CaptureFunds(order.OrderId, order.CustomerId, Scrapbook.TransactionId));
        await EventSource.NextOfType<FundsCaptured>();

        await _messageBroker.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
        await EventSource.NextOfType<OrderConfirmationEmailSent>();

        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }

    public class OrderScrapbook : RScrapbook
    {
        public Guid TransactionId { get; set; } = Guid.NewGuid();
    }
}
