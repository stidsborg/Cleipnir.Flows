using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Messaging;
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
    public async Task FailingActionFlowCompletesWithError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FailingUnitActionFlow>();
        
        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FallingUnitActionFlows(flowsContainer);

        FailingUnitActionFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<FatalWorkflowException<TimeoutException>>(() =>
            flows.Run("someInstanceId", "someParameter")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingUnitActionFlow.ShouldThrow = false;
        await controlPanel.Restart();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class FallingUnitActionFlows : Flows<FailingUnitActionFlow, string>
    {
        public FallingUnitActionFlows(FlowsContainer flowsContainer) 
            : base(nameof(FailingUnitActionFlow), flowsContainer, options: null) { }
    }
    
    public class FailingUnitActionFlow : Flow<string>
    {
        public static bool ShouldThrow = true;
        
        public override Task Run(string param)
        {
            return ShouldThrow 
                ? Task.FromException<TimeoutException>(new TimeoutException()) 
                : Task.CompletedTask;
        }
    }
    
    [TestMethod]
    public async Task FailingFuncFlowCompletesWithError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FailingUnitFuncFlow>();
        
        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FallingUnitFuncFlows(flowsContainer);

        FailingUnitFuncFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<FatalWorkflowException<TimeoutException>>(() =>
            flows.Run("someInstanceId", "someParameter")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingUnitFuncFlow.ShouldThrow = false;
        await controlPanel.Restart();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class FallingUnitFuncFlows : Flows<FailingUnitFuncFlow, string, string>
    {
        public FallingUnitFuncFlows(FlowsContainer flowsContainer) 
            : base(nameof(FailingUnitFuncFlow), flowsContainer, options: null) { }
    }
    
    public class FailingUnitFuncFlow : Flow<string, string>
    {
        public static bool ShouldThrow = true;
        
        public override Task<string> Run(string param)
        {
            return ShouldThrow 
                ? Task.FromException<string>(new TimeoutException()) 
                : param.ToTask();
        }
    }
    
    [TestMethod]
    public async Task FailingParamlessFlowCompletesWithError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FailingUnitParamlessFlow>();
        
        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FailingUnitParamlessFlows(flowsContainer);

        FailingUnitParamlessFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<FatalWorkflowException<TimeoutException>>(() =>
            flows.Run("someInstanceId")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingUnitParamlessFlow.ShouldThrow = false;
        await controlPanel.Restart();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }

    private class FailingUnitParamlessFlows : Flows<FailingUnitParamlessFlow>
    {
        public FailingUnitParamlessFlows(FlowsContainer flowsContainer) 
            : base(nameof(FailingUnitParamlessFlow), flowsContainer, options: null) { }
    }
    
    public class FailingUnitParamlessFlow : Flow
    {
        public static bool ShouldThrow = true;
        
        public override Task Run()
        {
            return ShouldThrow 
                ? Task.FromException<string>(new TimeoutException()) 
                : Task.CompletedTask;
        }
    }
    
    [TestMethod]
    public async Task FlowCanBeCreatedWithInitialState()
    {
        var flowsContainer = FlowsContainer.Create();
        var flow = new InitialStateFlow();
        var flows = flowsContainer.RegisterAnonymousFlow<InitialStateFlow, string>(
            flowFactory: () => flow
        );

        await flows.Run(
            "SomeInstanceId",
            param: "SomeParam",
            new InitialState(
                [new MessageAndIdempotencyKey("InitialMessageValue")],
                [new InitialEffect("InitialEffectId", "InitialEffectValue")]
            )
        );
        
        flow.InitialEffectValue.ShouldBe("InitialEffectValue");
        flow.InitialMessageValue.ShouldBe("InitialMessageValue");
    }

    private class InitialStateFlow : Flow<string>
    {
        public string? InitialEffectValue { get; set; }
        public string? InitialMessageValue { get; set; }
        
        public override async Task Run(string _)
        {
            InitialEffectValue = await Effect.Get<string>("InitialEffectId");
            InitialMessageValue = await Messages.OfType<string>().First();
        }
    }
}