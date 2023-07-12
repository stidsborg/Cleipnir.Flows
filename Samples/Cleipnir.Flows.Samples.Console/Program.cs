using Cleipnir.Flows.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.Console;

public static class Program
{
    private static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<OrderFlow>();
        serviceCollection.AddTransient<ScrapbooklessFlow>();

        var flows = new FlowsContainer(
            new InMemoryFlowStore(),
            serviceCollection.BuildServiceProvider(),
            new Options().UseMiddleware(new OwnMiddleware())
        );
        
        var orderFlows = new OrderFlows(flows);
        await orderFlows.Run(instanceId: "MK-12345", param: "MK-12345");

        var scrapbooklessFlows = new ScrapbooklessFlows(flows);
        await scrapbooklessFlows.Run("hello world", "hello world");
    }
}