namespace Cleipnir.Flows.Sample.Presentation.Solutions.B_OrderFlowMessaging.Other;

public class PaymentProviderStub
{
    private readonly MessageBroker _messageBroker;

    public PaymentProviderStub(MessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
        messageBroker.Subscribe(MessageHandler);
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
        await _messageBroker.Send(response);
    }
}