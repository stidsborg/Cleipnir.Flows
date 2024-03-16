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
        serviceCollection.AddTransient<SimpleFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<SimpleFlow, string, int>(nameof(SimpleFlow));
        var result = await flows.Run("someInstanceId", "someParameter");
        result.ShouldBe(1);
        
        SimpleFlow.InstanceId.ShouldBe("someInstanceId");
        SimpleFlow.ExecutedWithParameter.ShouldBe("someParameter");

        var controlPanel = await flows.ControlPanel(instanceId: "someInstanceId");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Succeeded);
        controlPanel.Result.ShouldBe(1);
    }

    private class SimpleFlow : Flow<string, int>
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
        serviceCollection.AddTransient<MessageDrivenFlow>();

        var flowStore = new InMemoryFunctionStore();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider()
        );

        var flows = flowsContainer.CreateFlows<MessageDrivenFlow, string, int>(nameof(MessageDrivenFlow));

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
    
    private class MessageDrivenFlow : Flow<string, int>
    {
        public override async Task<int> Run(string param)
        {
            var next = await Messages.FirstOfType<int>();
            return next;
        }
    }
}