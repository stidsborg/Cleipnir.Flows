using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.CrossCutting;

public delegate Task<Result<TResult>> Next<TFlow, TParam, TScrapbook, TResult>(
    TParam param,
    TScrapbook scrapbook,
    Context context
);

public interface IMiddleware
{
    public Task<Result<TResult>> Run<TFlow, TParam, TScrapbook, TResult>(
        TParam param,
        TScrapbook scrapbook,
        Context context,
        Next<TFlow, TParam, TScrapbook, TResult> next
    ) where TParam : notnull where TScrapbook : RScrapbook, new();
}