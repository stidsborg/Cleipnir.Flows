﻿using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Route = Cleipnir.ResilientFunctions.Domain.Route;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;

public class MessageDrivenOrderFlow(Bus bus) : Flow<Order>,
    ISubscription<FundsReserved>,
    ISubscription<ProductsShipped>,
    ISubscription<FundsCaptured>,
    ISubscription<OrderConfirmationEmailSent>
{
    #region Routing
    public static RoutingInfo Correlate(FundsReserved msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    public static RoutingInfo Correlate(ProductsShipped msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    public static RoutingInfo Correlate(FundsCaptured msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    public static RoutingInfo Correlate(OrderConfirmationEmailSent msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    #endregion
    
    public override async Task Run(Order order)
    {
        Console.WriteLine("MessageDriven-OrderFlow Started");
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        await Messages.FirstOfType<FundsReserved>();

        await ShipProducts(order);
        var productsShipped = await Messages.FirstOfType<ProductsShipped>();
        
        await CaptureFunds(order, transactionId);
        await Messages.FirstOfType<FundsCaptured>();

        await SendOrderConfirmationEmail(order, productsShipped.TrackAndTraceNumber);
        await Messages.FirstOfType<OrderConfirmationEmailSent>();
        
        Console.WriteLine("MessageDriven-OrderFlow Completed");
    }

    #region MessagePublishers
    private Task ReserveFunds(Order order, Guid transactionId)
        => Effect.Capture(
            "ReserveFunds",
            async () =>
            {
                Console.WriteLine("Reserving funds");
                await bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId));
            });

    private Task ShipProducts(Order order)
        => Effect.Capture(
            "ShipProducts",
            async () =>
            {
                Console.WriteLine("Shipping Products");
                await bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
            });

    private Task CaptureFunds(Order order, Guid transactionId)
        => Effect.Capture(
            "CaptureFunds",
            async () =>
            {
                Console.WriteLine("Capturing funds");
                await bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId));
            });

    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Effect.Capture(
            "SendOrderConfirmationEmail",
            async () =>
            {
                Console.WriteLine("Sending Order-confirmation Email");
                await bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber));
            });
    #endregion
}
