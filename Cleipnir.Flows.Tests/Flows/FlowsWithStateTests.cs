using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class FlowsWithStateTests
{
    [TestMethod]
    public async Task ParamlessFlowWithStateCanBeFetchedAfterExecution()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<ParamlessWithStateFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new ParamlessWithStateFlows(flowsContainer);
        await flows.Run("someInstanceId");

        var state = await flows.GetState("someInstanceId");
        state.ShouldNotBeNull();
        state.Boolean.ShouldBeTrue();
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }
    
    [TestMethod]
    public async Task ActionFlowWithStateCanBeFetchedAfterExecution()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<ActionWithStateFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new ActionWithStateFlows(flowsContainer);
        await flows.Run("someInstanceId", "someParameter");

        var state = await flows.GetState("someInstanceId");
        state.ShouldNotBeNull();
        state.Value.ShouldBe("someParameter");
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        
        var flowState = controlPanel.States.Get<FuncWithStateFlow.WorkflowState>();
        flowState.ShouldNotBeNull();
        flowState.Value.ShouldBe("someParameter");
    }
    
    [TestMethod]
    public async Task FuncFlowWithStateCanBeFetchedAfterExecution()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FuncWithStateFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FuncWithStateFlows(flowsContainer);
        await flows.Run("someInstanceId", "someParameter");

        var state = await flows.GetState("someInstanceId");
        state.ShouldNotBeNull();
        state.Value.ShouldBe("someParameter");
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Result.ShouldBe("someParameter");
        controlPanel.Status.ShouldBe(Status.Succeeded);
        
        var flowState = controlPanel.States.Get<FuncWithStateFlow.WorkflowState>();
        flowState.ShouldNotBeNull();
        flowState.Value.ShouldBe("someParameter");
    }
}

public class ActionWithStateFlow : Flow<string>, IExposeState<ActionWithStateFlow.WorkflowState>
{
    public required WorkflowState State { get; init; }
    
    public override Task Run(string param)
    {
        State.Value = param;
        return Task.CompletedTask;
    }

    public class WorkflowState : FlowState
    {
        public string Value { get; set; } = "";
    }
}

public class FuncWithStateFlow : Flow<string, string>, IExposeState<FuncWithStateFlow.WorkflowState>
{
    public required WorkflowState State { get; init; }
    
    public override Task<string> Run(string param)
    {
        State.Value = param;
        return Task.FromResult(param);
    }

    public class WorkflowState : FlowState
    {
        public string Value { get; set; } = "";
    }
}

public class ParamlessWithStateFlow : Flow, IExposeState<ParamlessWithStateFlow.WorkflowState>
{
    public required WorkflowState State { get; init; }
    
    public override Task Run()
    {
        State.Boolean = true;
        return Task.CompletedTask;
    }

    public class WorkflowState : FlowState
    {
        public bool Boolean { get; set; }
    }
}