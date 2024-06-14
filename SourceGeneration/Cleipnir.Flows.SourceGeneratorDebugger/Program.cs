using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.SourceGeneratorDebugger;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<OuterClass.FunFlow>();

        var container = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new FunFlows(container);
        await flows.Run("test");

        await Task.CompletedTask;
        //var funFlows = new FunFlows(container);
        //await funFlows.Run("SomeInstance", "SomeParam");

        //var state = await funFlows.GetState("SomeInstance");
        //Console.WriteLine("Fetched State-value: " + state!.Value);
    }
}


public class OuterClass
{
    public class FunFlow : Flow // IHaveState<FunFlow.FunFlowState>
    {
        public required FunFlowState State { get; init; }
    
        public override Task Run()
        {
            return Task.CompletedTask;
        }
        
        public class FunFlowState : WorkflowState
        {
            public string Value { get; set; } = "";
        }
    }
}