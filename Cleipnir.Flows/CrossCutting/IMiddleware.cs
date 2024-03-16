using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.CrossCutting;

public delegate Task<Result<TResult>> Next<TFlow, TParam, TResult>(
    TParam param,
    Workflow workflow
);

public interface IMiddleware
{
    public Task<Result<TResult>> Run<TFlow, TParam, TResult>(
        TParam param,
        Workflow workflow,
        Next<TFlow, TParam, TResult> next
    ) where TParam : notnull;
}