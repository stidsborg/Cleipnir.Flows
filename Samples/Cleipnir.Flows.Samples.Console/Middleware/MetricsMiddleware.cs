using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using IMiddleware = Cleipnir.Flows.CrossCutting.IMiddleware;

namespace Cleipnir.Flows.Sample.ConsoleApp.Middleware;

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

    public async Task<Result<TResult>> Run<TFlow, TParam, TResult>(
        TParam param, 
        Workflow workflow, 
        Next<TFlow, TParam, TResult> next) where TParam : notnull
    {
        var started = (await workflow.Effect.TryGet<bool>(id: "Started")).HasValue;
        if (started)
            IncrementRestartedFlowsCounter();
        else
            await workflow.Effect.Upsert("Started", true);
        
        var result = await next(param, workflow);
        if (result.Outcome == Outcome.Fail)
            IncrementFailedFlowsCounter();
        else if (result.Outcome == Outcome.Succeed)
            IncrementCompletedFlowsCounter();
        
        return result;
    }
}