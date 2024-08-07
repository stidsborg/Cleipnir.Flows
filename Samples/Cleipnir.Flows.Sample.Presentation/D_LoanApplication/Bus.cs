﻿namespace Cleipnir.Flows.Sample.Presentation.D_LoanApplication;

public static class Bus
{
    private static readonly List<Func<CommandAndEvents, Task>> _subscribers = new();
    private static readonly object _lock = new();

    public static void Subscribe(Func<CommandAndEvents, Task> handler)
    {
        lock (_lock)
            _subscribers.Add(handler);
    }
    
    public static Task Publish(CommandAndEvents msg)
    {
        Console.WriteLine("MESSAGE_QUEUE SENDING: " + msg.GetType());
        Task.Run(async () =>
        {
            List<Func<CommandAndEvents, Task>> subscribers;
            lock (_lock)
                subscribers = _subscribers.ToList();

            foreach (var subscriber in subscribers)
                await subscriber(msg);
        });
        
        return Task.CompletedTask;
    }
}