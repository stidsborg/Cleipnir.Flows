using Cleipnir.ResilientFunctions.Domain;
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
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<SimpleFuncFlow, string, int>(nameof(SimpleFuncFlow));
        var result = await flows.Run("someInstanceId", "someParameter");
        result.ShouldBe(1);
        
        SimpleFuncFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleFuncFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe(1);
    }

    public class SimpleFuncFlow : Flow<string, int>
    {
        public static string? ExecutedWithParameter { get; set; }
        public static string? InstanceId { get; set; } 

        public override async Task<int> Run(string param)
        {
            await Task.Delay(1);
            ExecutedWithParameter = param;
            InstanceId = Workflow.FunctionId.InstanceId.ToString();

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
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<MessageDrivenFuncFlow, string, int>(nameof(MessageDrivenFuncFlow));

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
    
    public class MessageDrivenFuncFlow : Flow<string, int>
    {
        public override async Task<int> Run(string param)
        {
            var next = await Messages.FirstOfType<int>();
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
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<FailingFuncFlow, string, string>(nameof(FailingFuncFlow));

        FailingFuncFlow.ShouldThrow = true;
        
        await Should.ThrowAsync<ArgumentException>(() =>
            flows.Run("someInstanceId", "someParameter")
        );
        
        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Failed);

        FailingFuncFlow.ShouldThrow = false;
        await controlPanel.ReInvoke();

        await controlPanel.Refresh();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe("someParameter");
    }
    
    public class FailingFuncFlow : Flow<string, string>
    {
        public static bool ShouldThrow = true;
        
        public override async Task<string> Run(string param)
        {
            if (ShouldThrow)
                throw new ArgumentException(param);

            return param;
        }
    }
}