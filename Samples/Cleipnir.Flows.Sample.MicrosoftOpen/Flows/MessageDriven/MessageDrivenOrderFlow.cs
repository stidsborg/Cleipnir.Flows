using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;
using Cleipnir.ResilientFunctions.Domain;
using Route = Cleipnir.ResilientFunctions.Domain.Route;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;

public class MessageDrivenOrderFlow(Bus bus) : 
    Flow<Order>, 
    ISubscription<FundsReserved, ProductsShipped, FundsCaptured, OrderConfirmationEmailSent>
{
    #region Routing
    public static RoutingInfo Correlate(FundsReserved msg) => Route.To(msg.OrderId);
    public static RoutingInfo Correlate(ProductsShipped msg) => Route.To(msg.OrderId);
    public static RoutingInfo Correlate(FundsCaptured msg) => Route.To(msg.OrderId);
    public static RoutingInfo Correlate(OrderConfirmationEmailSent msg) => Route.To(msg.OrderId);
    #endregion
    
    public override async Task Run(Order order)
    {
        await Capture(() => Console.WriteLine("MessageDriven-OrderFlow Started"));
        var transactionId = await Capture(Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        await Message<FundsReserved>();

        await ShipProducts(order);
        var productsShipped = await Message<ProductsShipped>();
        
        await CaptureFunds(order, transactionId);
        await Message<FundsCaptured>();

        await SendOrderConfirmationEmail(order, productsShipped.TrackAndTraceNumber);
        await Message<OrderConfirmationEmailSent>();
        
        await Capture(() => Console.WriteLine("MessageDriven-OrderFlow Completed"));
    }

    #region MessagePublishers

    private Task ReserveFunds(Order order, Guid transactionId)
        => Capture(
            () => bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId))
        );
    private Task ShipProducts(Order order)
        => Capture(
            () => bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds))
        );
    private Task CaptureFunds(Order order, Guid transactionId)
        => Capture(
            () => bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId))
        );
    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Capture(
            () => bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber))
        );

    #endregion
}
