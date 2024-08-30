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

    public FlowsContainer(IFunctionStore flowStore, IServiceProvider serviceProvider, Options options)
    {
        ServiceProvider = serviceProvider;
        
        if (options.UnhandledExceptionHandler == null && serviceProvider.GetService<ILogger>() != null)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            options = new Options(
                    unhandledExceptionHandler: ex => logger.LogError(ex, "Unhandled exception in Cleipnir Flows"),
                    options.RetentionPeriod, 
                    options.RetentionCleanUpFrequency,
                    options.LeaseLength, 
                    options.EnableWatchdogs,
                    options.WatchdogCheckFrequency,
                    options.MessagesPullFrequency,
                    options.MessagesDefaultMaxWaitForCompletion,
                    options.DelayStartup, 
                    options.MaxParallelRetryInvocations, 
                    options.Serializer
                );
        }
             
        FunctionRegistry = new FunctionsRegistry(flowStore, options.MapToSettings());
        Middlewares = options.Middlewares
            .Select(m => m switch
            {
                MiddlewareInstance middlewareInstance => middlewareInstance.Middleware,
                MiddlewareType middlewareType => (IMiddleware) serviceProvider.GetRequiredService(middlewareType.Type),
                _ => throw new ArgumentOutOfRangeException(nameof(m))
            })
            .ToList();
    }
    
    public void Dispose() => FunctionRegistry.Dispose();
    public Task ShutdownGracefully(TimeSpan? maxWait = null) => FunctionRegistry.ShutdownGracefully(maxWait);
}