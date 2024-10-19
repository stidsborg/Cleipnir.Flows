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

        var state = await (await flows.ControlPanel("someInstanceId"))!.States.Get<ParamlessWithStateFlow.WorkflowState>();
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

        var state = await (await flows.ControlPanel("someInstanceId"))!.States.Get<ActionWithStateFlow.WorkflowState>();
        state.ShouldNotBeNull();
        state.Value.ShouldBe("someParameter");
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        
        var flowState = await controlPanel.States.Get<FuncWithStateFlow.WorkflowState>();
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

        var state = await (await flows.ControlPanel("someInstanceId"))!.States.Get<FuncWithStateFlow.WorkflowState>();
        state.ShouldNotBeNull();
        state.Value.ShouldBe("someParameter");
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Result.ShouldBe("someParameter");
        controlPanel.Status.ShouldBe(Status.Succeeded);
        
        var flowState = await controlPanel.States.Get<FuncWithStateFlow.WorkflowState>();
        flowState.ShouldNotBeNull();
        flowState.Value.ShouldBe("someParameter");
    }
}

[GenerateFlows]
public class ActionWithStateFlow : Flow<string>
{
    public override async Task Run(string param)
    {
        var state = await Workflow.States.CreateOrGetDefault<WorkflowState>();
        state.Value = param;
        await state.Save();
    }

    public class WorkflowState : FlowState
    {
        public string Value { get; set; } = "";
    }
}

[GenerateFlows]
public class FuncWithStateFlow : Flow<string, string>
{
    public override async Task<string> Run(string param)
    {
        var state = await Workflow.States.CreateOrGetDefault<WorkflowState>();
        state.Value = param;
        await state.Save();
        return param;
    }

    public class WorkflowState : FlowState
    {
        public string Value { get; set; } = "";
    }
}

[GenerateFlows]
public class ParamlessWithStateFlow : Flow
{
    public override async Task Run()
    {
        var state = await Workflow.States.CreateOrGetDefault<WorkflowState>();
        state.Boolean = true;
        await state.Save();
    }

    public class WorkflowState : FlowState
    {
        public bool Boolean { get; set; }
    }
}