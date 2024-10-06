using Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions.Commands;
using Cleipnir.ResilientFunctions.Helpers;
using MassTransit;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console;

[GenerateFlows]
public class OrderFlow(IBus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = await Capture(Guid.NewGuid);
        try
        {
            await ReserveFunds(order, transactionId);
            await Message<FundsReserved>();

            await ShipProducts(order);
            var trackAndTraceNumber = await Message<ProductsShipped>()
                .SelectAsync(s => s.TrackAndTraceNumber);

            await CaptureFunds(order, transactionId);
            await Message<FundsCaptured>();

            await SendOrderConfirmationEmail(order, trackAndTraceNumber);
            await Message<OrderConfirmationEmailSent>();
        }
        catch (Exception e) when (e is not SuspendInvocationException)
        {
            await Compensate(order, transactionId);
        }
    }

    private async Task Compensate(Order order, Guid transactionId)
    {
        if (await Effect.GetStatus("ShipProducts") != WorkStatus.NotStarted)
            await CancelShipment(order);

        if (await Effect.GetStatus("CaptureFunds") != WorkStatus.NotStarted)
            await ReverseTransaction(order, transactionId);
        else if (await Effect.GetStatus("ReserveFunds") != WorkStatus.NotStarted)
            await CancelReservation(order, transactionId);
    }

    private Task ReserveFunds(Order order, Guid transactionId)
        => Capture(
            "ReserveFunds",
            () => bus.Publish(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId))
        );

    private Task ShipProducts(Order order)
        => Capture(
            "ShipProducts",
            () => bus.Publish(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds))
        );

    private Task CaptureFunds(Order order, Guid transactionId)
        => Capture(
            "CaptureFunds",
            () => bus.Publish(new CaptureFunds(order.OrderId, order.CustomerId, transactionId))
        );

    private Task SendOrderConfirmationEmail(Order order, string trackAndTraceNumber)
        => Capture(
            "SendOrderConfirmationEmail",
            () => bus.Publish(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId, trackAndTraceNumber))
        );

    private Task CancelShipment(Order order)
        => Capture(() => bus.Publish(new CancelShipment(order.OrderId)));
    private Task ReverseTransaction(Order order, Guid transactionId)
        => Capture(() => bus.Publish(new ReverseTransaction(order.OrderId, transactionId)));
    private Task CancelReservation(Order order, Guid transactionId)
        => Capture(() => bus.Publish(new CancelFundsReservation(order.OrderId, transactionId)));
}
