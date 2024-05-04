namespace Cleipnir.Flows.Sample.Presentation.B_OrderFlow_Messaging;

public class EmailServiceStub 
{
    private readonly Bus _bus;

    public EmailServiceStub(Bus bus)
    {
        _bus = bus;
        bus.Subscribe(MessageHandler);
    }

    private async Task MessageHandler(EventsAndCommands message)
    {
        if (message is not SendOrderConfirmationEmail command)
            return;

        await Task.Delay(1_000);
        await _bus.Send(new OrderConfirmationEmailSent(command.OrderId, command.CustomerId));
    }
}