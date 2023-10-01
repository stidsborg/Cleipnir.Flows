namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging.Solution;

public class EmailServiceStub 
{
    private readonly MessageBroker _messageBroker;

    public EmailServiceStub(MessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
        messageBroker.Subscribe(MessageHandler);
    }

    private async Task MessageHandler(EventsAndCommands message)
    {
        if (message is not SendOrderConfirmationEmail command)
            return;

        await Task.Delay(1_000);
        await _messageBroker.Send(new OrderConfirmationEmailSent(command.OrderId, command.CustomerId));
    }
}