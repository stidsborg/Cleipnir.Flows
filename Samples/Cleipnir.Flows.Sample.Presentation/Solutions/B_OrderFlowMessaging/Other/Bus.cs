﻿using Serilog;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.B_OrderFlowMessaging.Other;

public class Bus
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
        Log.Logger.ForContext<Bus>().Information($"Sending: {msg.GetType()}");
        Task.Run(async () =>
        {
            List<Func<EventsAndCommands, Task>> subscribers;
            lock (_lock)
                subscribers = _subscribers.ToList();

            foreach (var subscriber in subscribers)
                await subscriber(msg);
        });
        
        return Task.CompletedTask;
    }
}