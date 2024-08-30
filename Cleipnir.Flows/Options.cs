using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.ParameterSerialization;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions;

namespace Cleipnir.Flows;

public class Options
{
    public static Options Default { get; } = new();
    
    internal Action<FlowTypeException>? UnhandledExceptionHandler { get; }
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
    internal List<MiddlewareInstanceOrType> Middlewares  { get; } = new();

    public Options(
        Action<FlowTypeException>? unhandledExceptionHandler = null, 
        TimeSpan? retentionPeriod = null,
        TimeSpan? retentionCleanUpFrequency = null,
        TimeSpan? leaseLength = null, 
        bool? enableWatchdogs = null,
        TimeSpan? watchdogCheckFrequency = null,
        TimeSpan? messagesPullFrequency = null,
        TimeSpan? messagesDefaultMaxWaitForCompletion = null,
        TimeSpan? delayStartup = null, 
        int? maxParallelRetryInvocations = null, 
        ISerializer? serializer = null
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

    public Options Merge(Options options)
    {
        var merged = new Options(
            UnhandledExceptionHandler ?? options.UnhandledExceptionHandler,
            RetentionPeriod ?? options.RetentionPeriod,
            RetentionCleanUpFrequency ?? options.RetentionCleanUpFrequency,
            LeaseLength ?? options.LeaseLength,
            EnableWatchdogs ?? options.EnableWatchdogs,
            WatchdogCheckFrequency ?? options.WatchdogCheckFrequency,
            MessagesPullFrequency ?? options.MessagesPullFrequency,
            MessagesDefaultMaxWaitForCompletion ?? options.MessagesDefaultMaxWaitForCompletion,
            DelayStartup ?? options.DelayStartup,
            MaxParallelRetryInvocations ?? options.MaxParallelRetryInvocations,
            Serializer ?? options.Serializer
        );
        
        if (Middlewares.Any())
            foreach (var middleware in Middlewares)
                merged.Middlewares.Add(middleware);
        else
            foreach (var middleware in options.Middlewares)
                merged.Middlewares.Add(middleware);

        return merged;
    }

    internal Settings MapToSettings()
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
            Serializer
        );
}