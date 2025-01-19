using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class ParamlessFlowsTests
{
    [TestMethod]
    public async Task SimpleFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<SimpleParamlessFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new SimpleParamlessFlows(flowsContainer);
        await flows.Run("someInstanceId");
        
        SimpleParamlessFlow.InstanceId.ShouldBe("someInstanceId");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class SimpleParamlessFlows : Flows<SimpleParamlessFlow>
    {
        public SimpleParamlessFlows(FlowsContainer flowsContainer) 
            : base(nameof(SimpleParamlessFlow), flowsContainer, options: null) { }
    }
    
    public class SimpleParamlessFlow : Flow
    {
        public static string? InstanceId { get; set; } 

        public override async Task Run()
        {
            await Task.Delay(1);
            InstanceId = Workflow.FlowId.Instance.ToString();
        }
    }
    
    [TestMethod]
    public async Task EventDrivenFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<EventDrivenParamlessFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new EventDrivenParamlessFlows(flowsContainer); 
        await flows.Schedule("someInstanceId");

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

    private class EventDrivenParamlessFlows : Flows<EventDrivenParamlessFlow>
    {
        public EventDrivenParamlessFlows(FlowsContainer flowsContainer) 
            : base(nameof(EventDrivenParamlessFlow), flowsContainer, options: null) { }
    }
    
    public class EventDrivenParamlessFlow : Flow
    {
        public override async Task Run()
        {
            await Messages.FirstOfType<int>(maxWait: TimeSpan.MaxValue);
        }
    }
    
    [TestMethod]
    public async Task FailingFlowCompletesWithError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FailingParamlessFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FailingParamlessFlows(flowsContainer);
        FailingParamlessFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<FatalWorkflowException<TimeoutException>>(() =>
            flows.Run("someInstanceId")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingParamlessFlow.ShouldThrow = false;
        await controlPanel.Restart();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class FailingParamlessFlows : Flows<FailingParamlessFlow>
    {
        public FailingParamlessFlows(FlowsContainer flowsContainer) 
            : base(nameof(FailingParamlessFlow), flowsContainer, options: null) { }
    }
    
    public class FailingParamlessFlow : Flow
    {
        public static bool ShouldThrow = true;
        
        public override Task Run()
        {
            return ShouldThrow 
                ? Task.FromException<TimeoutException>(new TimeoutException()) 
                : Task.CompletedTask;
        }
    }
}