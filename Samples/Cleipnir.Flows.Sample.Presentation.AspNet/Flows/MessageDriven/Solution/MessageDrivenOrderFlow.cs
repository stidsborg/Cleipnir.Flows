using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Solution;

public class MessageDrivenOrderFlow(Bus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        await Capture(() => Console.WriteLine("MessageDriven-OrderFlow Started"));
        var transactionId = await Effect.Capture(Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        var reservation = await EitherMessage<FundsReserved, FundsReservationFailed>();
        if (reservation.HasSecond)
            await Compensate(reservation.Second, order, transactionId);
        
        await ShipProducts(order);
        var productsShipped = await EitherMessage<ProductsShipped, ProductsShipmentFailed>();
        if (productsShipped.HasSecond)
            await Compensate(productsShipped.Second, order, transactionId);
        
        await CaptureFunds(order, transactionId);
        var capture = await EitherMessage<FundsCaptured, FundsCaptureFailed>();
        if (capture.HasSecond)
            await Compensate(capture.Second, order, transactionId);
        
        await SendOrderConfirmationEmail(order, productsShipped.First);
        var emailSent = await EitherMessage<OrderConfirmationEmailSent, OrderConfirmationEmailSent>();
        if (emailSent.HasSecond)
            await Compensate(emailSent.Second, order, transactionId);
        
        await Capture(() => Console.WriteLine("MessageDriven-OrderFlow Completed"));
    }

    private async Task Compensate(object failureMessage, Order order, Guid transactionId)
    {
        var messages = 
            (await Messages.TakeUntil(msg => msg == failureMessage).ToList())
            .Append(failureMessage)
            .ToList();

        if (messages.Any(msg => msg is OrderConfirmationEmailFailed))
            await CancelProductsShipment(order);
        if (messages.Any(msg => msg is FundsCaptureFailed))
            await ReserveFunds(order, transactionId);
        else
            await CancelFundsReservation(order, transactionId);

        throw new Exception("Flow failed");
    }
    
    private Task ReserveFunds(Order order, Guid transactionId) 
        => Capture(() => bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId)));
    private Task ShipProducts(Order order)
        => Capture(() => bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds)));
    private Task CaptureFunds(Order order, Guid transactionId)
        => Capture(() => bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId)));
    private Task SendOrderConfirmationEmail(Order order, ProductsShipped productsShipped)
        => Capture(() => bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, productsShipped.TrackAndTraceNumber)));
    private Task CancelProductsShipment(Order order)
        => Capture(() => bus.Send(new CancelProductShipment(order.OrderId)));
    private Task CancelFundsReservation(Order order, Guid transactionId)
        => Capture(() => bus.Send(new CancelFundsReservation(order.OrderId, transactionId)));
}
