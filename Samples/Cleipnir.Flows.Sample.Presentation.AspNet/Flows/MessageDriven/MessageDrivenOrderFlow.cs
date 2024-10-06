using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;

[GenerateFlows]
public class MessageDrivenOrderFlow(Bus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        //todo throw OrderProcessingException
        var transactionId = await Capture(Guid.NewGuid);

        await ReserveFunds(order, transactionId);
        
        await ShipProducts(order);
        
        await CaptureFunds(order, transactionId);

        var trackAndTraceNumber = "";
        await SendOrderConfirmationEmail(order, trackAndTraceNumber);
    }

    #region MessagePublishers

    private Task ReserveFunds(Order order, Guid transactionId)
        => Capture(
            () => bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId))
        );
    private Task CancelFundsReservation(Order order, Guid transactionId)
        => Capture(
            () => bus.Send(new CancelFundsReservation(order.OrderId, transactionId))
        );
    private Task ShipProducts(Order order)
        => Capture(
            () => bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds))
        );
    private Task CancelProductsShipment(Order order)
        => Capture(
            () => bus.Send(new CancelProductsShipment(order.OrderId))
        );
    private Task CaptureFunds(Order order, Guid transactionId)
        => Capture(
            () => bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, transactionId))
        );
    private Task ReverseTransaction(Order order, Guid transactionId)
        => Capture(
            () => bus.Send(new ReverseTransaction(order.OrderId, transactionId))
        );
    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Capture(
            () => bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber))
        );

    #endregion
}
