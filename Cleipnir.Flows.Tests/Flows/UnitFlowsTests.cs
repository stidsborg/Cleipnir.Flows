using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;
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

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<SimpleFlow, string>(nameof(SimpleFlow));
        await flows.Run("someInstanceId", "someParameter");
        
        SimpleFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class SimpleFlow : Flow<string>
    {
        public static string? ExecutedWithParameter { get; set; }
        public static string? InstanceId { get; set; } 

        public override async Task Run(string param)
        {
            await Task.Delay(1);
            ExecutedWithParameter = param;
            InstanceId = Workflow.FunctionId.InstanceId.ToString();
        }
    }
    
    [TestMethod]
    public async Task EventDrivenFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<EventDrivenFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<EventDrivenFlow, string>(nameof(EventDrivenFlow));

        await flows.Schedule("someInstanceId", "someParameter");

        await Task.Delay(10);
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Executing);
        
        var eventSourceWriter = flows.MessageWriter("someInstanceId");
        await eventSourceWriter.AppendMessage(2);

        await controlPanel.WaitForCompletion();

        await controlPanel.Refresh();
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }
    
    private class EventDrivenFlow : Flow<string>
    {
        public override async Task Run(string param)
        {
            await Messages.FirstOfType<int>();
        }
    }
}