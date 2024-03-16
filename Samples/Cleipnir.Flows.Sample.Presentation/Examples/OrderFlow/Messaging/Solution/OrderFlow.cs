using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging.Solution;

public class OrderFlow : Flow<Order>
{
    private readonly MessageBroker _messageBroker;
    private ILogger Logger { get; } = Log.Logger.ForContext<OrderFlow>();

    public OrderFlow(MessageBroker messageBroker) => _messageBroker = messageBroker;

    public override async Task Run(Order order)
    {
        Logger.Information($"Processing of order '{order.OrderId}' started");

        var transactionId = await Effect.CreateOrGet<Guid>("TransactionId", Guid.NewGuid());
        
        await _messageBroker.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId));
        await Messages.FirstOfType<FundsReserved>();

        await _messageBroker.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
        var either = await Messages.OfTypes<ProductsShipped, ProductsShipmentFailed>().First();
        if (either.HasSecond)
        {
            await _messageBroker.Send(new CancelFundsReservation(order.OrderId, transactionId));
            return;
        }

        await _messageBroker.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId));
        await Messages.FirstOfType<FundsCaptured>();

        await _messageBroker.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
        await Messages.FirstOfType<OrderConfirmationEmailSent>();

        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }
}
