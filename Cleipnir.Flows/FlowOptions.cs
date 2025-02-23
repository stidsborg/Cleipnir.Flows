using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public class FlowOptions
{
    public static FlowOptions Default { get; } = new();
    
    internal TimeSpan? RetentionPeriod { get; }
    internal bool? EnableWatchdogs { get; }
    internal int? MaxParallelRetryInvocations { get; }
    internal TimeSpan? MessagesDefaultMaxWaitForCompletion { get; }
    internal List<MiddlewareInstanceOrType> Middlewares  { get; } = new();

    public FlowOptions(
        TimeSpan? retentionPeriod = null,
        bool? enableWatchdogs = null,
        TimeSpan? messagesDefaultMaxWaitForCompletion = null,  
        int? maxParallelRetryInvocations = null
    )
    {
        RetentionPeriod = retentionPeriod;
        EnableWatchdogs = enableWatchdogs;
        MessagesDefaultMaxWaitForCompletion = messagesDefaultMaxWaitForCompletion;
        MaxParallelRetryInvocations = maxParallelRetryInvocations;
    }

    public FlowOptions UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware
    {
        Middlewares.Add(new MiddlewareType(typeof(TMiddleware)));
        return this;
    }

    public FlowOptions UseMiddleware(IMiddleware middleware) 
    {
        Middlewares.Add(new MiddlewareInstance(middleware));
        return this;
    }

    public FlowOptions Merge(Options options)
    {
        var merged = new FlowOptions(
            RetentionPeriod ?? options.RetentionPeriod,
            EnableWatchdogs ?? options.EnableWatchdogs,
            MessagesDefaultMaxWaitForCompletion ?? options.MessagesDefaultMaxWaitForCompletion,
            MaxParallelRetryInvocations ?? options.MaxParallelRetryInvocations
        );
        
        if (Middlewares.Any())
            foreach (var middleware in Middlewares)
                merged.Middlewares.Add(middleware);
        else
            foreach (var middleware in options.Middlewares)
                merged.Middlewares.Add(middleware);

        return merged;
    }

    internal LocalSettings MapToLocalSettings()
        => new(RetentionPeriod, EnableWatchdogs, MessagesDefaultMaxWaitForCompletion, MaxParallelRetryInvocations);
}