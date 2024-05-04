using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.ConsoleApp.AtMostOnce;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<AtMostOnceFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider()
        );

        var flows = new AtMostOnceFlows(flowsContainer);
        var rocketId = "AZ-213";
        await flows.Run(rocketId, rocketId);
    }
}