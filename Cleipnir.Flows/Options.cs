using System;
using System.Collections.Generic;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.ParameterSerialization;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions;

namespace Cleipnir.Flows;

public class Options
{
    public static Options Default { get; } = new();
    
    internal Action<RFunctionException>? UnhandledExceptionHandler { get; }
    internal TimeSpan? RetentionPeriod { get; }
    internal TimeSpan? RetentionCleanUpFrequency { get; }
    internal TimeSpan? LeaseLength { get; }
    internal bool? EnableWatchdogs { get; }
    internal TimeSpan? WatchdogCheckFrequency { get; }
    internal TimeSpan? DelayStartup { get; }
    internal int? MaxParallelRetryInvocations { get; }
    internal TimeSpan? MessagesPullFrequency { get; }
    internal TimeSpan? MessagesDefaultMaxWaitForCompletion { get; }
    internal ISerializer? Serializer { get; }
    internal IEnumerable<RoutingInformation>? Routes { get; }
    internal List<MiddlewareInstanceOrType> Middlewares  { get; } = new();

    public Options(
        Action<RFunctionException>? unhandledExceptionHandler = null, 
        TimeSpan? retentionPeriod = null,
        TimeSpan? retentionCleanUpFrequency = null,
        TimeSpan? leaseLength = null, 
        bool? enableWatchdogs = null,
        TimeSpan? watchdogCheckFrequency = null,
        TimeSpan? messagesPullFrequency = null,
        TimeSpan? messagesDefaultMaxWaitForCompletion = null,
        TimeSpan? delayStartup = null, 
        int? maxParallelRetryInvocations = null, 
        ISerializer? serializer = null,
        IEnumerable<RoutingInformation>? routes = null
    )
    {
        UnhandledExceptionHandler = unhandledExceptionHandler;
        WatchdogCheckFrequency = watchdogCheckFrequency;
        LeaseLength = leaseLength;
        RetentionPeriod = retentionPeriod;
        RetentionCleanUpFrequency = retentionCleanUpFrequency;
        EnableWatchdogs = enableWatchdogs;
        MessagesPullFrequency = messagesPullFrequency;
        MessagesDefaultMaxWaitForCompletion = messagesDefaultMaxWaitForCompletion;
        DelayStartup = delayStartup;
        MaxParallelRetryInvocations = maxParallelRetryInvocations;
        Serializer = serializer;
        Routes = routes;
    }

    public Options UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware
    {
        Middlewares.Add(new MiddlewareType(typeof(TMiddleware)));
        return this;
    }

    public Options UseMiddleware(IMiddleware middleware) 
    {
        Middlewares.Add(new MiddlewareInstance(middleware));
        return this;
    }

    internal Settings MapToRFunctionsSettings()
        => new(
            UnhandledExceptionHandler,
            RetentionPeriod,
            RetentionCleanUpFrequency,
            LeaseLength,
            EnableWatchdogs,
            WatchdogCheckFrequency,
            MessagesPullFrequency,
            MessagesDefaultMaxWaitForCompletion,
            DelayStartup,
            MaxParallelRetryInvocations,
            Serializer,
            Routes
        );
}