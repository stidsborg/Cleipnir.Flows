﻿namespace Cleipnir.Flows.Sample.Presentation.Solutions.B_OrderFlowMessaging.Other;

public class LogisticsServiceStub 
{
    private readonly Bus _bus;

    public LogisticsServiceStub(Bus bus)
    {
        _bus = bus;
        bus.Subscribe(MessageHandler);
    }

    private async Task MessageHandler(EventsAndCommands message)
    {
        if (message is not ShipProducts command)
            return;

        await Task.Delay(1_000);
        await _bus.Send(new ProductsShipped(command.OrderId, TrackAndTrace: Guid.NewGuid().ToString()));
    }
}