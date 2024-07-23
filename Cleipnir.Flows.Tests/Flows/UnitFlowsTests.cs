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
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new SimpleUnitFlows(flowsContainer);
        await flows.Run("someInstanceId", "someParameter");
        
        SimpleUnitFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleUnitFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class SimpleUnitFlows : Flows<SimpleUnitFlow, string>
    {
        public SimpleUnitFlows(FlowsContainer flowsContainer) 
            : base(nameof(SimpleUnitFlow), flowsContainer, options: null) { }
    } 
    
    public class SimpleUnitFlow : Flow<string>
    {
        public static string? ExecutedWithParameter { get; set; }
        public static string? InstanceId { get; set; } 

        public override async Task Run(string param)
        {
            await Task.Delay(1);
            ExecutedWithParameter = param;
            InstanceId = Workflow.FlowId.Instance.ToString();
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
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new EventDrivenUnitFlows(flowsContainer);

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
    
    private class EventDrivenUnitFlows : Flows<EventDrivenUnitFlow, string>
    {
        public EventDrivenUnitFlows(FlowsContainer flowsContainer) 
            : base(nameof(EventDrivenUnitFlow), flowsContainer, options: null) { }
    }

    public class EventDrivenUnitFlow : Flow<string>
    {
        public override async Task Run(string param)
        {
            await Messages.FirstOfType<int>(maxWait: TimeSpan.MaxValue);
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
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FallingUnitFlows(flowsContainer);

        FailingUnitFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<TimeoutException>(() =>
            flows.Run("someInstanceId", "someParameter")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingUnitFlow.ShouldThrow = false;
        await controlPanel.Restart();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class FallingUnitFlows : Flows<FailingUnitFlow, string>
    {
        public FallingUnitFlows(FlowsContainer flowsContainer) 
            : base(nameof(FailingUnitFlow), flowsContainer, options: null) { }
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