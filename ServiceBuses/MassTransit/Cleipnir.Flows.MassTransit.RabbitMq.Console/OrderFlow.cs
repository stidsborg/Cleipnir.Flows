using Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using MassTransit;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console;

public class OrderFlow(IBus bus) : Flow,
    ISubscription<Order>,
    ISubscription<ConsumeContext<FundsReserved>>,
    ISubscription<ConsumeContext<ProductsShipped>>,
    ISubscription<ConsumeContext<FundsCaptured>>,
    ISubscription<ConsumeContext<OrderConfirmationEmailSent>>
{
    #region Routing
    public static RoutingInfo Correlate(Order order) => Route.To(order.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<FundsReserved> msg) => Route.To(msg.Message.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<ProductsShipped> msg) => Route.To(msg.Message.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<FundsCaptured> msg) => Route.To(msg.Message.OrderId);
    public static RoutingInfo Correlate(ConsumeContext<OrderConfirmationEmailSent> msg) => Route.To(msg.Message.OrderId);
    #endregion
    
    public override async Task Run()
    {
        var order = await Messages.FirstOfType<Order>();
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        await Messages.FirstOfType<FundsReserved>();

        await ShipProducts(order);
        var productsShipped = await Messages.FirstOfType<ProductsShipped>();
        
        await CaptureFunds(order, transactionId);
        await Messages.FirstOfType<FundsCaptured>();

        await SendOrderConfirmationEmail(order, productsShipped.TrackAndTraceNumber);
        await Messages.FirstOfType<OrderConfirmationEmailSent>();
    }

    #region MessagePublishers

    private Task ReserveFunds(Order order, Guid transactionId)
        => Effect.Capture(
            "ReserveFunds",
            async () => await bus.Publish(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId))
        );

    private Task ShipProducts(Order order)
        => Effect.Capture(
            "ShipProducts",
            async () => await bus.Publish(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds))
        );
    
    private Task CaptureFunds(Order order, Guid transactionId)
        => Effect.Capture(
            "CaptureFunds",
            async () => await bus.Publish(new CaptureFunds(order.OrderId, order.CustomerId, transactionId))
        );

    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Effect.Capture(
            "SendOrderConfirmationEmail",
            async () => await bus.Publish(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber))
        );

    #endregion
}
