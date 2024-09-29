namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

public class Bus(Solution.MessageDrivenOrderFlows flows)
{
    private readonly List<Func<EventsAndCommands, Task>> _subscribers = new();
    private readonly object _lock = new();

    public void Subscribe(Func<EventsAndCommands, Task> handler)
    {
        lock (_lock)
            _subscribers.Add(handler);
    }
    
    public Task Send(EventsAndCommands msg)
    {
        Task.Run(async () =>
        {
            List<Func<EventsAndCommands, Task>> subscribers;
            lock (_lock)
                subscribers = _subscribers.ToList();

            foreach (var subscriber in subscribers)
                await subscriber(msg);

            await (msg switch
            {
                FundsCaptured m => flows.SendMessage(m.OrderId, m),
                FundsCaptureFailed m => flows.SendMessage(m.OrderId, m),
                FundsReservationCancelled m => flows.SendMessage(m.OrderId, m),
                FundsReservationFailed m => flows.SendMessage(m.OrderId, m),
                FundsReserved m => flows.SendMessage(m.OrderId, m),
                OrderConfirmationEmailFailed m => flows.SendMessage(m.OrderId, m),
                OrderConfirmationEmailSent m => flows.SendMessage(m.OrderId, m),
                ProductsShipmentFailed m => flows.SendMessage(m.OrderId, m),
                ProductsShipped m => flows.SendMessage(m.OrderId, m),
                _ => Task.CompletedTask
            });
        });
        
        return Task.CompletedTask;
    }
}