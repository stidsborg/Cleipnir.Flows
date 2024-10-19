using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions.Commands;

namespace Cleipnir.Flows.CrossCutting;

public static class CallChain
{
    public static Next<TFlow, TParam, TResult> Create<TFlow, TParam, TResult>(
        List<IMiddleware> middlewares, 
        Func<TParam, Workflow, Task<TResult>> runFlow) 
        where TParam : notnull
    {
        Next<TFlow, TParam, TResult> currNext =
            async (p, w) =>
            {
                try
                {
                    var result = await runFlow(p, w);
                    return new Result<TResult>(result);
                }
                catch (SuspendInvocationException)
                {
                    return new Result<TResult>(Suspend.Invocation);
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

            currNext = (p, w) => middleware.Run(p, w, next);
        }

        return currNext;
    }
}