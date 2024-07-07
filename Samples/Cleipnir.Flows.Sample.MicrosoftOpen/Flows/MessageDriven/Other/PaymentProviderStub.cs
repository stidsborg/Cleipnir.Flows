namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

public class PaymentProviderStub(Bus bus)
{
    public void Initialize() => bus.Subscribe(MessageHandler);

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
        await bus.Send(response);
    }
}