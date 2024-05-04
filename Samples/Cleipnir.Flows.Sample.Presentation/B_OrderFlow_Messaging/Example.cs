using Cleipnir.ResilientFunctions.Storage;

namespace Cleipnir.Flows.Sample.Presentation.B_OrderFlow_Messaging;

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
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider()
        );

        var orderFlows = new OrderFlows(flowsContainer);
        
        messageBroker.Subscribe(async msg =>
        {
            switch (msg)
            {
                case FundsCaptured e:
                    await orderFlows.MessageWriter(e.OrderId).AppendMessage(e, idempotencyKey: $"{nameof(FundsCaptured)}.{e.OrderId}");
                    break;
                case FundsReservationCancelled e:
                    await orderFlows.MessageWriter(e.OrderId).AppendMessage(e, idempotencyKey: $"{nameof(FundsReservationCancelled)}.{e.OrderId}");
                    break;
                case FundsReserved e:
                    await orderFlows.MessageWriter(e.OrderId).AppendMessage(e, idempotencyKey: $"{nameof(FundsReserved)}.{e.OrderId}");
                    break;
                case OrderConfirmationEmailSent e:
                    await orderFlows.MessageWriter(e.OrderId).AppendMessage(e, idempotencyKey: $"{nameof(OrderConfirmationEmailSent)}.{e.OrderId}");
                    break;
                case ProductsShipped e:
                    await orderFlows.MessageWriter(e.OrderId).AppendMessage(e, idempotencyKey: $"{nameof(ProductsShipped)}.{e.OrderId}");
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