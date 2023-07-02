using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cleipnir.Flows;

public class FlowsContainer : IDisposable
{
    internal readonly IServiceProvider _serviceProvider;
    internal readonly RFunctions _rFunctions;
    internal readonly List<IMiddleware> _middlewares;

    public FlowsContainer(IFlowStore flowStore, IServiceProvider serviceProvider, Options? options = null)
    {
        _serviceProvider = serviceProvider;
        if (options != null && options.UnhandledExceptionHandler == null && serviceProvider.GetService<ILogger>() != null)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            options = new Options(
                    unhandledExceptionHandler: excp => logger.LogError(excp, "Unhandled exception in Cleipnir Flows"),
                    options.CrashedCheckFrequency, 
                    options.PostponedCheckFrequency, 
                    options.TimeoutCheckFrequency,
                    options.SuspensionCheckFrequency,
                    options.EventSourcePullFrequency,
                    options.DelayStartup, 
                    options.MaxParallelRetryInvocations, 
                    options.Serializer
                );
        }
             
        _rFunctions = new RFunctions(flowStore, options?.MapToRFunctionsSettings());
        _middlewares = options?.Middlewares
            .Select(m => m switch
            {
                MiddlewareInstance middlewareInstance => middlewareInstance.Middleware,
                MiddlewareType middlewareType => (IMiddleware)serviceProvider.GetRequiredService(middlewareType.Type),
                _ => throw new ArgumentOutOfRangeException(nameof(m))
            })
            .ToList() ?? new List<IMiddleware>();
    }

    public Flows<TFlow, TParam, TScrapbook> RegisterFlow<TFlow, TParam, TScrapbook>(string flowName, TFlow flow)
    where TFlow : Flow<TParam, TScrapbook>
    where TParam : notnull
    where TScrapbook : RScrapbook, new()
    {
        var flowRegistration = new Flows<TFlow, TParam, TScrapbook>(flowName, flowsContainer: this);
        return flowRegistration;
    }

    public Flows<TFlow, TParam, TScrapbook> RegisterFlow<TFlow, TParam, TScrapbook>(string flowName, Func<(TFlow, IDisposable)> flowResolver)
        where TFlow : Flow<TParam, TScrapbook>
        where TParam : notnull
        where TScrapbook : RScrapbook, new()
    {
        var flowRegistration = new Flows<TFlow, TParam, TScrapbook>(flowName, flowsContainer: this);
        return flowRegistration;
    }
    
    public Flows<TFlow, TParam, TScrapbook> RegisterFlow<TFlow, TParam, TScrapbook>(string flowName, Func<TFlow> flowResolver)
        where TFlow : Flow<TParam, TScrapbook>
        where TParam : notnull
        where TScrapbook : RScrapbook, new()
    {
        var flowRegistration = new Flows<TFlow, TParam, TScrapbook>(flowName, flowsContainer: this);
        return flowRegistration;
    }
    
    public Flows<TFlow, TParam, TScrapbook> RegisterFlow<TFlow, TParam, TScrapbook>(string flowName)
        where TFlow : Flow<TParam, TScrapbook>
        where TParam : notnull
        where TScrapbook : RScrapbook, new()
    {
        var flowRegistration = new Flows<TFlow, TParam, TScrapbook>(flowName, flowsContainer: this);
        return flowRegistration;
    }
    
    public Flows<TFlow, TParam, TScrapbook, TResult> RegisterFlow<TFlow, TParam, TScrapbook, TResult>(string flowName)
        where TFlow : Flow<TParam, TScrapbook, TResult>
        where TParam : notnull
        where TScrapbook : RScrapbook, new()
    {
        var flowRegistration = new Flows<TFlow, TParam, TScrapbook, TResult>(flowName, flowsContainer: this);
        return flowRegistration;
    }

    public void Dispose() => _rFunctions.Dispose();

    public Task ShutdownGracefully(TimeSpan? maxWait = null) => _rFunctions.ShutdownGracefully(maxWait);
}