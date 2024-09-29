using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;

public class MessageDrivenOrderFlow(Bus bus) : Flow<Order>
{
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
