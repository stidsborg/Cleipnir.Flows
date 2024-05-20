using Cleipnir.Flows.Sample.Presentation.Solutions.B_OrderFlowMessaging.Other;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.B_OrderFlowMessaging;

public class OrderFlow(MessageBroker messageBroker) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = await Effect.CreateOrGet("TransactionId", Guid.NewGuid());
        
        await messageBroker.Send(ReserveFunds(order, transactionId));
        await Messages.FirstOfType<FundsReserved>();

        await messageBroker.Send(ShipProducts(order));
        var trackAndTrace = await Messages
            .FirstOfType<ProductsShipped>()
            .SelectAsync(msg => msg.TrackAndTrace);
        
        await messageBroker.Send(CaptureFunds(order, transactionId));
        await Messages.FirstOfType<FundsCaptured>();

        await messageBroker.Send(SendOrderConfirmationEmail(order, trackAndTrace));
        await Messages.FirstOfType<OrderConfirmationEmailSent>();
    }

    #region MessageFactories

    private static ReserveFunds ReserveFunds(Order order, Guid transactionId)
        => new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId);
    private static ShipProducts ShipProducts(Order order)
        => new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds);
    private static CaptureFunds CaptureFunds(Order order, Guid transactionId)
        => new CaptureFunds(order.OrderId, order.CustomerId, transactionId);
    private static SendOrderConfirmationEmail SendOrderConfirmationEmail(Order order, string trackAndTrace)
        => new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTrace);

    #endregion
}
