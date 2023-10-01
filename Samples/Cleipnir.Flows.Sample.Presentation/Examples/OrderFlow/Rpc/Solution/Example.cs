using Cleipnir.Flows.Persistence;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Rpc.Solution;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<IPaymentProviderClient, PaymentProviderClientStub>();
        serviceCollection.AddTransient<IEmailClient, EmailClientStub>();
        serviceCollection.AddTransient<ILogisticsClient, LogisticsClientStub>();
        serviceCollection.AddTransient<OrderFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFlowStore(),
            serviceCollection.BuildServiceProvider()
        );

        var orderFlows = new OrderFlows(flowsContainer);
        await orderFlows.Run(
            "MK-54321",
            new Order("MK-54321", CustomerId: Guid.NewGuid(), ProductIds: new [] { Guid.NewGuid(), Guid.NewGuid() }, TotalPrice: 120M)
        );
    }
}