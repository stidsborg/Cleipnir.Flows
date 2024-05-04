using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.ConsoleApp.Postpone;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<PostponeFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider()
        );

        var flows = new PostponeFlows(flowsContainer);
        var orderId = "MK-54321";
        await flows.Schedule(orderId, orderId);

        var controlPanel = await flows.ControlPanel(orderId);
        while (controlPanel!.Status == Status.Succeeded)
        {
            await Task.Delay(TimeSpan.FromMinutes(10));
            await controlPanel.Refresh();
        }
    }
}