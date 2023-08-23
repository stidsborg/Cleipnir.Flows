using Cleipnir.Flows.Persistence;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging;

public static class Example
{
    public static async Task Execute()
    {
        var messageBroker = new MessageBroker();
        var emailService = new EmailServiceStub(messageBroker);
        var logisticsService = new LogisticsServiceStub(messageBroker);
        var paymentProviderService = new PaymentProviderStub(messageBroker);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(messageBroker);
        serviceCollection.AddTransient<OrderFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFlowStore(),
            serviceCollection.BuildServiceProvider()
        );

        var orderFlows = new OrderFlows(flowsContainer);
        
        messageBroker.Subscribe(async msg =>
        {
            switch (msg)
            {
                case FundsCaptured e:
                    await orderFlows.EventSourceWriter(e.OrderId).AppendEvent(e, idempotencyKey: $"{nameof(FundsCaptured)}.{e.OrderId}");
                    break;
                case FundsReservationCancelled e:
                    await orderFlows.EventSourceWriter(e.OrderId).AppendEvent(e, idempotencyKey: $"{nameof(FundsReservationCancelled)}.{e.OrderId}");
                    break;
                case FundsReserved e:
                    await orderFlows.EventSourceWriter(e.OrderId).AppendEvent(e, idempotencyKey: $"{nameof(FundsReserved)}.{e.OrderId}");
                    break;
                case OrderConfirmationEmailSent e:
                    await orderFlows.EventSourceWriter(e.OrderId).AppendEvent(e, idempotencyKey: $"{nameof(OrderConfirmationEmailSent)}.{e.OrderId}");
                    break;
                case ProductsShipped e:
                    await orderFlows.EventSourceWriter(e.OrderId).AppendEvent(e, idempotencyKey: $"{nameof(ProductsShipped)}.{e.OrderId}");
                    break;

                default:
                    return;
            }
        });
        
        var order = new Order(
            OrderId: "MK-4321",
            CustomerId: Guid.NewGuid(),
            ProductIds: new[] { Guid.NewGuid(), Guid.NewGuid() },
            TotalPrice: 123.5M
        );
        
        await orderFlows.Run(order.OrderId, order);
    }
}