using Cleipnir.Flows.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.Console.AtMostOnce;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<AtMostOnceFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFlowStore(),
            serviceCollection.BuildServiceProvider()
        );

        var flows = new AtMostOnceFlows(flowsContainer);
        var rocketId = "AZ-213";
        await flows.Run(rocketId, rocketId);
    }
}