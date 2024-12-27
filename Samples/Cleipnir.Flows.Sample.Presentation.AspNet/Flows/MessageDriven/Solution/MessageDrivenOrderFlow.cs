using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Reactive.Utilities;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Solution;

[GenerateFlows]
public class MessageDrivenOrderFlow(Bus bus) : Flow<Order>
{
   public override async Task Run(Order order)
    {
        var transactionId = await Effect.Capture(Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        var reservation = await MessageOrTimeout<FundsReserved, FundsReservationFailed>(TimeSpan.FromSeconds(10));
        if (!reservation.HasFirst)
            await CleanUp(FailedAt.FundsReserved, order, transactionId);

        await ShipProducts(order);
        var productsShipped = await MessageOrTimeout<ProductsShipped, ProductsShipmentFailed>(TimeSpan.FromMinutes(5));
        if (!productsShipped.HasFirst)
            await CleanUp(FailedAt.ProductsShipped, order, transactionId);
        var trackAndTraceNumber = productsShipped.First.TrackAndTraceNumber;
        
        await CaptureFunds(order, transactionId);
        var capture = await MessageOrTimeout<FundsCaptured, FundsCaptureFailed>(TimeSpan.FromSeconds(10));
        if (!capture.HasFirst)
            await CleanUp(FailedAt.FundsCaptured, order, transactionId);
        
        await SendOrderConfirmationEmail(order, trackAndTraceNumber);
        await MessageOrTimeout<OrderConfirmationEmailSent, OrderConfirmationEmailFailed>(TimeSpan.FromSeconds(10));
    }
    
    private Task<EitherOrNone<T1, T2>> MessageOrTimeout<T1, T2>(TimeSpan timeout) =>
        Messages
            .TakeUntilTimeout(timeout)
            .OfTypes<T1, T2>()
            .FirstOrNone();

    private async Task CleanUp(FailedAt failedAt, Order order, Guid transactionId)
    {
        switch (failedAt) 
        {
            case FailedAt.FundsReserved:
                await CancelFundsReservation(order, transactionId);
                break;
            case FailedAt.ProductsShipped:
                await CancelFundsReservation(order, transactionId);
                await CancelProductsShipment(order);
                break;
            case FailedAt.FundsCaptured:
                await CancelProductsShipment(order);
                break;
            case FailedAt.OrderConfirmationEmailSent:
                //we accept this failure without cleaning up
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(failedAt), failedAt, null);
        }

        throw new OrderProcessingException($"Order processing failed at: '{failedAt}'");
    }

    private enum FailedAt
    {
        FundsReserved,
        ProductsShipped,
        FundsCaptured,
        OrderConfirmationEmailSent,
    }
    
    private Task ReserveFunds(Order order, Guid transactionId) 
        => Capture(() => bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId)));
    private Task ShipProducts(Order order)
        => Capture(() => bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds)));
    private Task CaptureFunds(Order order, Guid transactionId)
        => Capture(() => bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId)));
    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Capture(() => bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber)));
    private Task CancelProductsShipment(Order order)
        => Capture(() => bus.Send(new CancelProductsShipment(order.OrderId)));
    private Task CancelFundsReservation(Order order, Guid transactionId)
        => Capture(() => bus.Send(new CancelFundsReservation(order.OrderId, transactionId)));
}
