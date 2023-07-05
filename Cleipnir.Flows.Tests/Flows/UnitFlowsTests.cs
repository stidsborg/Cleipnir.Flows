using Cleipnir.Flows.Persistence;
using Cleipnir.Flows.Reactive;
using Cleipnir.ResilientFunctions.Domain;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class UnitFlowsTests
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

        var flows = flowsContainer.CreateFlows<SimpleFlow, string, RScrapbook>(nameof(SimpleFlow));
        await flows.Run("someInstanceId", "someParameter");
        
        SimpleFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class SimpleFlow : Flow<string, RScrapbook>
    {
        public static string? ExecutedWithParameter { get; set; }
        public static string? InstanceId { get; set; } 

        public override async Task Run(string param)
        {
            await Task.Delay(1);
            ExecutedWithParameter = param;
            InstanceId = Context.FunctionId.InstanceId.ToString();
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

        var flows = flowsContainer.CreateFlows<EventDrivenFlow, string, RScrapbook>(nameof(EventDrivenFlow));

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
    }
    
    private class EventDrivenFlow : Flow<string, RScrapbook>
    {
        public override async Task Run(string param)
        {
            await EventSource.NextOfType<int>();
        }
    }
}