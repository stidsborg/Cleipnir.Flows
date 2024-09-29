namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

public class EmailServiceStub(Bus bus)
{
    public void Initialize() => bus.Subscribe(MessageHandler);

    private async Task MessageHandler(EventsAndCommands message)
    {
        if (message is not SendOrderConfirmationEmail command)
            return;

        await Task.Delay(1_000);
        await bus.Send(new OrderConfirmationEmailSent(command.OrderId, command.CustomerId));
    }
}