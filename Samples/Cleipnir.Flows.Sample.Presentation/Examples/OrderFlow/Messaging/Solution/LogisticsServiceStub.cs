namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging.Solution;

public class LogisticsServiceStub 
{
    private readonly MessageBroker _messageBroker;

    public LogisticsServiceStub(MessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
        messageBroker.Subscribe(MessageHandler);
    }

    private async Task MessageHandler(EventsAndCommands message)
    {
        if (message is not ShipProducts command)
            return;

        await Task.Delay(1_000);
        await _messageBroker.Send(new ProductsShipped(command.OrderId));
    }
}