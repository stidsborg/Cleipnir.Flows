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
        serviceCollection.AddTransient<SimpleUnitFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<SimpleUnitFlow, string>(nameof(SimpleUnitFlow));
        await flows.Run("someInstanceId", "someParameter");
        
        SimpleUnitFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleUnitFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    public class SimpleUnitFlow : Flow<string>
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
        serviceCollection.AddTransient<EventDrivenUnitFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<EventDrivenUnitFlow, string>(nameof(EventDrivenUnitFlow));

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
    
    public class EventDrivenUnitFlow : Flow<string>
    {
        public override async Task Run(string param)
        {
            await Messages.FirstOfType<int>();
        }
    }
    
    [TestMethod]
    public async Task FailingFlowCompletesWithError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FailingUnitFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<FailingUnitFlow, string>(nameof(FailingUnitFlow));

        FailingUnitFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<TimeoutException>(() =>
            flows.Run("someInstanceId", "someParameter")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingUnitFlow.ShouldThrow = false;
        await controlPanel.ReInvoke();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }
    
    public class FailingUnitFlow : Flow<string>
    {
        public static bool ShouldThrow = true;
        
        public override Task Run(string param)
        {
            return ShouldThrow 
                ? Task.FromException<TimeoutException>(new TimeoutException()) 
                : Task.CompletedTask;
        }
    }
}