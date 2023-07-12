using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using IMiddleware = Cleipnir.Flows.CrossCutting.IMiddleware;

namespace Cleipnir.Flows.Sample.Console.Middleware;

public class MetricsMiddleware : IMiddleware
{
    private Action IncrementCompletedFlowsCounter { get; }
    private Action IncrementFailedFlowsCounter { get; }
    private Action IncrementRestartedFlowsCounter { get; }

    public MetricsMiddleware(Action incrementCompletedFlowsCounter, Action incrementFailedFlowsCounter, Action incrementRestartedFlowsCounter)
    {
        IncrementCompletedFlowsCounter = incrementCompletedFlowsCounter;
        IncrementFailedFlowsCounter = incrementFailedFlowsCounter;
        IncrementRestartedFlowsCounter = incrementRestartedFlowsCounter;
    }

    public async Task<Result<TResult>> Run<TFlow, TParam, TScrapbook, TResult>(
        TParam param, 
        TScrapbook scrapbook, 
        Context context, 
        Next<TFlow, TParam, TScrapbook, TResult> next) where TParam : notnull where TScrapbook : RScrapbook, new()
    {
        if (context.InvocationMode == InvocationMode.Retry)
            IncrementRestartedFlowsCounter();
        
        var result = await next(param, scrapbook, context);
        if (result.Outcome == Outcome.Fail)
            IncrementFailedFlowsCounter();
        else if (result.Outcome == Outcome.Succeed)
            IncrementCompletedFlowsCounter();
        
        return result;
    }
}