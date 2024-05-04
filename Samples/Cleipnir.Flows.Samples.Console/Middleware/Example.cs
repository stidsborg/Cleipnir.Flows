using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Sample.ConsoleApp.Middleware;

public static class Example
{
    private static int _completedFlowsCounter;
    private static int _failedFlowsCounter;
    private static int _restartedFlowsCounter;
    
    public static async Task Do()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<MiddlewareFlow>();

        var flowsContainer = new FlowsContainer(
            new InMemoryFunctionStore(),
            serviceCollection.BuildServiceProvider(),
            new Options().UseMiddleware(
                new MetricsMiddleware(
                    incrementCompletedFlowsCounter: () => Interlocked.Increment(ref _completedFlowsCounter),
                    incrementFailedFlowsCounter: () => Interlocked.Increment(ref _failedFlowsCounter),
                    incrementRestartedFlowsCounter: () => Interlocked.Increment(ref _restartedFlowsCounter)
                )
            )
        );

        var flows = new MiddlewareFlows(flowsContainer);

        for (var i = 0; i < 20; i++)
            _ = flows.Run(i.ToString(), "parameter");

        await Task.Delay(1_000);
        
        System.Console.WriteLine("Completed Flows: " + _completedFlowsCounter);
        System.Console.WriteLine("Failed Flows: " + _failedFlowsCounter);
        System.Console.WriteLine("Restarted Flows: " + _restartedFlowsCounter);
    }
}