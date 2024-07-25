using MassTransit;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;

public class PaymentProviderStub(IBus bus) : IConsumer<CaptureFunds>, IConsumer<ReserveFunds>
{
    public Task Consume(ConsumeContext<CaptureFunds> context)
    {
        var command = context.Message;
        
        Task.Delay(1).ContinueWith(_ =>
            bus.Publish(
                new FundsCaptured(command.OrderId)
            )
        );
        
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<ReserveFunds> context)
    {
        var command = context.Message;

        Task.Delay(1).ContinueWith(_ =>
            bus.Publish(
                new FundsReserved(command.OrderId)
            )
        );
        
        return Task.CompletedTask;
    }
}