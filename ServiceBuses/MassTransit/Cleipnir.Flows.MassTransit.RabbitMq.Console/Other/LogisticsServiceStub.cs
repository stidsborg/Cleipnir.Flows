using MassTransit;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;

public class LogisticsServiceStub(IBus bus) : IConsumer<ShipProducts>
{
    public Task Consume(ConsumeContext<ShipProducts> context)
    {
        var command = context.Message;
        
        Task.Delay(1).ContinueWith(_ =>
            bus.Publish(
                new ProductsShipped(command.OrderId, TrackAndTraceNumber: Guid.NewGuid().ToString("N"))
            )
        );
        
        return Task.CompletedTask;
    }
}