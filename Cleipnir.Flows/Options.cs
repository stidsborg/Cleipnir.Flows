using System;
using System.Collections.Generic;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.ParameterSerialization;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions;

namespace Cleipnir.Flows;

public class Options
{
    internal Action<RFunctionException>? UnhandledExceptionHandler { get; }
    internal TimeSpan? CrashedCheckFrequency { get; }
    internal TimeSpan? PostponedCheckFrequency { get; }
    internal TimeSpan? TimeoutCheckFrequency { get; }
    internal TimeSpan? SuspensionCheckFrequency { get; }
    internal TimeSpan? EventSourcePullFrequency { get; }
    internal TimeSpan? DelayStartup { get; }
    internal int? MaxParallelRetryInvocations { get; }
    internal ISerializer? Serializer { get; }
    internal List<MiddlewareInstanceOrType> Middlewares  { get; } = new();

    public Options(
        Action<RFunctionException>? unhandledExceptionHandler = null, 
        TimeSpan? crashedCheckFrequency = null, 
        TimeSpan? postponedCheckFrequency = null, 
        TimeSpan? timeoutCheckFrequency = null,
        TimeSpan? suspensionCheckFrequency = null,
        TimeSpan? eventSourcePullFrequency = null,
        TimeSpan? delayStartup = null, 
        int? maxParallelRetryInvocations = null, 
        ISerializer? serializer = null
    )
    {
        UnhandledExceptionHandler = unhandledExceptionHandler;
        CrashedCheckFrequency = crashedCheckFrequency;
        PostponedCheckFrequency = postponedCheckFrequency;
        TimeoutCheckFrequency = timeoutCheckFrequency;
        SuspensionCheckFrequency = suspensionCheckFrequency;
        EventSourcePullFrequency = eventSourcePullFrequency;
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
    
    //todo use middleware from func?

    internal Settings MapToRFunctionsSettings()
        => new(
            UnhandledExceptionHandler,
            CrashedCheckFrequency,
            PostponedCheckFrequency,
            TimeoutCheckFrequency,
            SuspensionCheckFrequency,
            EventSourcePullFrequency,
            DelayStartup,
            MaxParallelRetryInvocations,
            Serializer,
            dependencyResolver: null
        );
}