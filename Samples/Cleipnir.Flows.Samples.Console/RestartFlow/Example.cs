using Cleipnir.Flows.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.Console.RestartFlow;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<RestartFailedFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFlowStore(),
            serviceCollection.BuildServiceProvider()
        );

        var flows = new RestartFailedFlows(flowsContainer);
        var flowId = "MK-54321";
        try
        {
            await flows.Run(flowId, ""); //invalid parameter    
        }
        catch (Exception)
        {
            // ignored
        }
        
        var controlPanel = await flows.ControlPanel(flowId);
        controlPanel!.Param = "valid parameter";
        await controlPanel.RunAgain();
    }
}