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
        serviceCollection.AddTransient<ParamlessFlowWithState>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new ParamlessFlowWithStates(flowsContainer);
        await flows.Run("someInstanceId");

        var state = await flows.GetState("someInstanceId");
        state.ShouldNotBeNull();
        state.Boolean.ShouldBeTrue();
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
    }
    
    public class ParamlessFlowWithState : Flow, IHaveState<ParamlessFlowWithState.FlowState>
    {
        public required FlowState State { get; init; }
    
        public override Task Run()
        {
            State.Boolean = true;
            return Task.CompletedTask;
        }

        public class FlowState : WorkflowState
        {
            public bool Boolean { get; set; }
        }
    }
    
    [TestMethod]
    public async Task ActionFlowWithStateCanBeFetchedAfterExecution()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<ActionFlowWithState>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new ActionFlowWithStates(flowsContainer);
        await flows.Run("someInstanceId", "someParameter");

        var state = await flows.GetState("someInstanceId");
        state.ShouldNotBeNull();
        state.Value.ShouldBe("someParameter");
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        
        var flowState = controlPanel.States.Get<FuncFlowWithState.FlowState>();
        flowState.ShouldNotBeNull();
        flowState.Value.ShouldBe("someParameter");
    }
    
    public class ActionFlowWithState : Flow<string>, IHaveState<ActionFlowWithState.FlowState>
    {
        public required FlowState State { get; init; }
    
        public override Task Run(string param)
        {
            State.Value = param;
            return Task.CompletedTask;
        }

        public class FlowState : WorkflowState
        {
            public string Value { get; set; } = "";
        }
    }
    
    [TestMethod]
    public async Task FuncFlowWithStateCanBeFetchedAfterExecution()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FuncFlowWithState>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FuncFlowWithStates(flowsContainer);
        await flows.Run("someInstanceId", "someParameter");

        var state = await flows.GetState("someInstanceId");
        state.ShouldNotBeNull();
        state.Value.ShouldBe("someParameter");
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Result.ShouldBe("someParameter");
        controlPanel.Status.ShouldBe(Status.Succeeded);
        
        var flowState = controlPanel.States.Get<FuncFlowWithState.FlowState>();
        flowState.ShouldNotBeNull();
        flowState.Value.ShouldBe("someParameter");
    }
    
    public class FuncFlowWithState : Flow<string, string>, IHaveState<FuncFlowWithState.FlowState>
    {
        public required FlowState State { get; init; }
    
        public override Task<string> Run(string param)
        {
            State.Value = param;
            return Task.FromResult(param);
        }

        public class FlowState : WorkflowState
        {
            public string Value { get; set; } = "";
        }
    }
}