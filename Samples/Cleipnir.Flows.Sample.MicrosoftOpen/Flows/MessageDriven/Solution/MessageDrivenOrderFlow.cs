using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Route = Cleipnir.ResilientFunctions.Domain.Route;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Solution;

public class MessageDrivenOrderFlow(Bus bus) : Flow<Order>,
    ISubscription<FundsReserved>,
    ISubscription<ProductsShipped>,
    ISubscription<FundsCaptured>,
    ISubscription<OrderConfirmationEmailSent>
{
    public static RoutingInfo Route(FundsReserved msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    public static RoutingInfo Route(ProductsShipped msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    public static RoutingInfo Route(FundsCaptured msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    public static RoutingInfo Route(OrderConfirmationEmailSent msg) => ResilientFunctions.Domain.Route.To(msg.OrderId);
    
    private static readonly TimeSpan? MaxWait = default; 
    
    public override async Task Run(Order order)
    {
        Console.WriteLine("MessageDriven-OrderFlow Started");
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);

        await Effect.Capture("ReserveFunds", () => ReserveFunds(order, transactionId));
        await Messages.FirstOfType<FundsReserved>(MaxWait);

        await Effect.Capture("ShipProducts", () => ShipProducts(order));
        var productsShipped = await Messages.FirstOfType<ProductsShipped>(MaxWait);
        
        await Effect.Capture("CaptureFunds", () => CaptureFunds(order, transactionId));
        await Messages.FirstOfType<FundsCaptured>(MaxWait);

        await Effect.Capture("SendOrderConfirmationEmail", () => SendOrderConfirmationEmail(order, productsShipped));
        await Messages.FirstOfType<OrderConfirmationEmailSent>(MaxWait);
        
        Console.WriteLine("MessageDriven-OrderFlow Completed");
    }
    
    private Task ReserveFunds(Order order, Guid transactionId) 
        => bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId));
    private Task ShipProducts(Order order)
        => bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
    private Task CaptureFunds(Order order, Guid transactionId)
        => bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId));
    private Task SendOrderConfirmationEmail(Order order, ProductsShipped productsShipped)
        => bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, productsShipped.TrackAndTraceNumber));
}
