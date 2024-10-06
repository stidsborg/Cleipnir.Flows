using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.SourceGeneratorDebugger;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<SimpleFlow>();

        var container = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new SimpleFlows(container);
        await flows.Run("SomeInstance");
    }
}

[GenerateFlows]
public class SimpleFlow : Flow, IExposeState<SimpleFlow.FlowState>
{
    public required FlowState State { get; init; }

    public override Task Run()
    {
        Console.WriteLine("Executing FunFlow");
        return Task.CompletedTask;
    }

    public class FlowState : ResilientFunctions.Domain.FlowState
    {
        public string Value { get; set; } = "";
    }
}