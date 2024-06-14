using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.ConsoleApp.AtLeastOnce;

public static class Example
{
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<AtLeastOnceFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider(),
            Options.Default
        );

        var flows = new AtLeastOnceFlows(flowsContainer);
        var hashCode = "¤SOME_#A$H";
        var solution = await flows.Run(hashCode, hashCode);
        System.Console.WriteLine("Solution was: " + solution);
    }
}