using Cleipnir.Flows.Persistence;
using Cleipnir.Flows.Reactive;
using Cleipnir.ResilientFunctions.Domain;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class FlowsWithResultTests
{
    [TestMethod]
    public async Task SimpleFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<SimpleFlow>();

        var flowStore = new InMemoryFlowStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<SimpleFlow, string, RScrapbook, int>(nameof(SimpleFlow));
        var result = await flows.Run("someInstanceId", "someParameter");
        result.ShouldBe(1);
        
        SimpleFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe(1);
    }

    private class SimpleFlow : Flow<string, RScrapbook, int>
    {
        public static string? ExecutedWithParameter { get; set; }
        public static string? InstanceId { get; set; } 

        public override async Task<int> Run(string param)
        {
            await Task.Delay(1);
            ExecutedWithParameter = param;
            InstanceId = Context.FunctionId.InstanceId.ToString();

            return 1;
        }
    }
    
    [TestMethod]
    public async Task EventDrivenFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<EventDrivenFlow>();

        var flowStore = new InMemoryFlowStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<EventDrivenFlow, string, RScrapbook, int>(nameof(EventDrivenFlow));

        await flows.Schedule("someInstanceId", "someParameter");

        await Task.Delay(10);
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Executing);

        var eventSourceWriter = flows.EventSourceWriter("someInstanceId");
        await eventSourceWriter.AppendEvent(2);

        await controlPanel.WaitForCompletion();

        await controlPanel.Refresh();
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe(2);
    }
    
    private class EventDrivenFlow : Flow<string, RScrapbook, int>
    {
        public override async Task<int> Run(string param)
        {
            var next = await EventSource.NextOfType<int>();
            return next;
        }
    }
}