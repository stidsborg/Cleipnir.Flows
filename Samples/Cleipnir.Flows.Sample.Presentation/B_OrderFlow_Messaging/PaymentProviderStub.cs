namespace Cleipnir.Flows.Sample.Presentation.B_OrderFlow_Messaging;

public class PaymentProviderStub
{
    private readonly Bus _bus;

    public PaymentProviderStub(Bus bus)
    {
        _bus = bus;
        bus.Subscribe(MessageHandler);
    }

    private async Task MessageHandler(EventsAndCommands message)
    {
        var response = message switch
        {
            CaptureFunds captureFunds => new FundsCaptured(captureFunds.OrderId),
            ReserveFunds reserveFunds => new FundsReserved(reserveFunds.OrderId),
            CancelFundsReservation cancelFundsReservation => new FundsReservationCancelled(cancelFundsReservation.OrderId),
            _ => default(EventsAndCommands)
        };
        if (response == null) return;

        await Task.Delay(1_000);
        await _bus.Send(response);
    }
}