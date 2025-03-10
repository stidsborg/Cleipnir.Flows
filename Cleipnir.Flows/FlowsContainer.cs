using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private readonly Dictionary<string, Type> _registeredFlows = new();
    private readonly Lock _lock = new();

    public FunctionsRegistry Functions => FunctionRegistry;
    
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

    internal void EnsureNoExistingRegistration(string flowName, Type flowType)
    {
        lock (_lock)
            if (_registeredFlows.TryGetValue(flowName, out var existingFlowType) && flowType != existingFlowType)
                throw new InvalidOperationException($"Flow with name '{flowName}' for type '{flowType}' has already been registered for different type: '{existingFlowType}'");
            else
                _registeredFlows[flowName] = flowType;
    }
    
    public void Dispose() => FunctionRegistry.Dispose();
    public Task ShutdownGracefully(TimeSpan? maxWait = null) => FunctionRegistry.ShutdownGracefully(maxWait);
    
    public static FlowsContainer Create(
        IServiceProvider? serviceProvider = null,
        IFunctionStore? functionStore = null, 
        Options? options = null) 
        => new(
            functionStore ?? new InMemoryFunctionStore(),
            serviceProvider ?? new ServiceCollection().BuildServiceProvider(),
            options ?? Options.Default
        );
}
