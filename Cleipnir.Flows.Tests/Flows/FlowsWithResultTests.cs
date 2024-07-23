using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;
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
        serviceCollection.AddTransient<SimpleFuncFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new SimpleFuncFlows(flowsContainer);
        var result = await flows.Run("someInstanceId", "someParameter");
        result.ShouldBe(1);
        
        SimpleFuncFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleFuncFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe(1);
    }
    
    private class SimpleFuncFlows : Flows<SimpleFuncFlow, string, int>
    {
        public SimpleFuncFlows(FlowsContainer flowsContainer) 
            : base(nameof(SimpleFuncFlow), flowsContainer, options: null) { }
    }
    
    public class SimpleFuncFlow : Flow<string, int>
    {
        public static string? ExecutedWithParameter { get; set; }
        public static string? InstanceId { get; set; } 

        public override async Task<int> Run(string param)
        {
            await Task.Delay(1);
            ExecutedWithParameter = param;
            InstanceId = Workflow.FlowId.Instance.ToString();

            return 1;
        }
    }
    
    [TestMethod]
    public async Task EventDrivenFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<MessageDrivenFuncFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new MessageDrivenFuncFlows(flowsContainer);
        await flows.Schedule("someInstanceId", "someParameter");

        await Task.Delay(10);
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Executing);

        var messageWriter = flows.MessageWriter("someInstanceId");
        await messageWriter.AppendMessage(2);

        await controlPanel.WaitForCompletion();

        await controlPanel.Refresh();
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe(2);
    }

    private class MessageDrivenFuncFlows : Flows<MessageDrivenFuncFlow, string, int>
    {
        public MessageDrivenFuncFlows(FlowsContainer flowsContainer) 
            : base(nameof(MessageDrivenFuncFlow), flowsContainer, options: null) { }
    }
    
    public class MessageDrivenFuncFlow : Flow<string, int>
    {
        public override async Task<int> Run(string param)
        {
            var next = await Messages.FirstOfType<int>(maxWait: TimeSpan.MaxValue);
            return next;
        }
    }
    
    [TestMethod]
    public async Task FailingFlowCompletesWithError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<FailingFuncFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options()
        );

        var flows = new FailingFuncFlows(flowsContainer);
        FailingFuncFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<ArgumentException>(() =>
            flows.Run("someInstanceId", "someParameter")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingFuncFlow.ShouldThrow = false;
        await controlPanel.Restart();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe("someParameter");
    }

    private class FailingFuncFlows : Flows<FailingFuncFlow, string, string>
    {
        public FailingFuncFlows(FlowsContainer flowsContainer) 
            : base(nameof(FailingFuncFlow), flowsContainer, options: null) { }
    }
    
    public class FailingFuncFlow : Flow<string, string>
    {
        public static bool ShouldThrow = true;
        
        public override Task<string> Run(string param)
        {
            if (ShouldThrow)
                return Task.FromException<string>(new ArgumentException(param));

            return param.ToTask();
        }
    }
}