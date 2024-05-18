using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cleipnir.Flows;

public class FlowsContainer : IDisposable
{
    internal readonly IServiceProvider ServiceProvider;
    internal readonly FunctionsRegistry FunctionRegistry;
    internal readonly List<IMiddleware> Middlewares;

    public FlowsContainer(IFunctionStore flowStore, IServiceProvider serviceProvider, Options? options = null)
    {
        ServiceProvider = serviceProvider;
        options ??= new Options();
        
        if (options.UnhandledExceptionHandler == null && serviceProvider.GetService<ILogger>() != null)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            options = new Options(
                    unhandledExceptionHandler: excp => logger.LogError(excp, "Unhandled exception in Cleipnir Flows"),
                    options.CrashedCheckFrequency, 
                    options.PostponedCheckFrequency, 
                    options.TimeoutCheckFrequency,
                    options.EventSourcePullFrequency,
                    options.DelayStartup, 
                    options.MaxParallelRetryInvocations, 
                    options.Serializer
                );
        }
             
        FunctionRegistry = new FunctionsRegistry(flowStore, options.MapToRFunctionsSettings());
        Middlewares = options.Middlewares
            .Select(m => m switch
            {
                MiddlewareInstance middlewareInstance => middlewareInstance.Middleware,
                MiddlewareType middlewareType => (IMiddleware)serviceProvider.GetRequiredService(middlewareType.Type),
                _ => throw new ArgumentOutOfRangeException(nameof(m))
            })
            .ToList();
    }

    public Flows<TFlow> CreateFlows<TFlow>(string flowName)
        where TFlow : Flow => new(flowName, flowsContainer: this);
    
    public Flows<TFlow, TParam> CreateFlows<TFlow, TParam>(string flowName)
        where TFlow : Flow<TParam> 
        where TParam : notnull => new(flowName, flowsContainer: this);
    
    public Flows<TFlow, TParam, TResult> CreateFlows<TFlow, TParam, TResult>(string flowName)
        where TFlow : Flow<TParam, TResult>
        where TParam : notnull => new(flowName, flowsContainer: this);
    

    public void Dispose() => FunctionRegistry.Dispose();

    public Task ShutdownGracefully(TimeSpan? maxWait = null) => FunctionRegistry.ShutdownGracefully(maxWait);
}