using MassTransit;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;

public class EmailServiceStub(IBus bus) : IConsumer<SendOrderConfirmationEmail>
{
    public Task Consume(ConsumeContext<SendOrderConfirmationEmail> context)
    {
        var command = context.Message;
        Task.Delay(1).ContinueWith(_ =>
            bus.Publish(new OrderConfirmationEmailSent(command.OrderId, command.CustomerId))
        );

        return Task.CompletedTask;
    }
}