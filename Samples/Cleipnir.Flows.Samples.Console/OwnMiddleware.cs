using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using IMiddleware = Cleipnir.Flows.CrossCutting.IMiddleware;

namespace Cleipnir.Flows.Sample.Console;

public class OwnMiddleware : IMiddleware
{
    public async Task<Result<TResult>> Run<TFlow, TParam, TScrapbook, TResult>(
        TParam param, 
        TScrapbook scrapbook, 
        Context context, 
        Next<TFlow, TParam, TScrapbook, TResult> next) where TParam : notnull where TScrapbook : RScrapbook, new()
    {
        System.Console.WriteLine("Inside middleware - before flow");
        var result = await next(param, scrapbook, context);
        System.Console.WriteLine("Inside middleware - after flow");

        return result;
    }
}