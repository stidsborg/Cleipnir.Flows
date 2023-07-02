using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions;

namespace Cleipnir.Flows.CrossCutting;

public static class CallChain
{
    public static Next<TFlow, TParam, TScrapbook, TResult> Create<TFlow, TParam, TScrapbook, TResult>(
        List<IMiddleware> middlewares, 
        Func<TParam, TScrapbook, Context, Task<TResult>> runFlow) 
        where TParam : notnull where TScrapbook : RScrapbook, new()
    {
        Next<TFlow, TParam, TScrapbook, TResult> currNext =
            async (p, s, c) =>
            {
                try
                {
                    var result = await runFlow(p, s, c);
                    return new Result<TResult>(result);
                }
                catch (SuspendInvocationException suspendInvocationException)
                {
                    return new Result<TResult>(Suspend.UntilAfter(suspendInvocationException.ExpectedEventCount));
                }
                catch (PostponeInvocationException postponeInvocationException)
                {
                    return new Result<TResult>(Postpone.Until(postponeInvocationException.PostponeUntil));
                }
                catch (Exception exception)
                {
                    return new Result<TResult>(exception);
                }
            };
        
        for (var i = middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = middlewares[i];
            var next = currNext;

            currNext = (p, s, c) => middleware.Run(p, s, c, next);
        }

        return currNext;
    }
}