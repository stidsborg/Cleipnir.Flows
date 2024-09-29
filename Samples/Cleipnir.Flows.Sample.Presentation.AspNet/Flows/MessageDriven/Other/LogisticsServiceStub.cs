namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

public class LogisticsServiceStub(Bus bus)
{
    public void Initialize() => bus.Subscribe(MessageHandler);
    
    private async Task MessageHandler(EventsAndCommands message)
    {
        if (message is not ShipProducts command)
            return;

        await Task.Delay(1_000);
        await bus.Send(
            new ProductsShipped(command.OrderId, TrackAndTraceNumber: Guid.NewGuid().ToString("N"))
        );
    }
}