using System;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Messaging;

namespace Cleipnir.Flows;

public abstract class BaseFlow
{
    public Workflow Workflow { get; init; } = null!;
    public Utilities Utilities => Workflow.Utilities;
    public Messages Messages => Workflow.Messages;
    public Effect Effect => Workflow.Effect;
    
    public Task<T> Capture<T>(string id, Func<Task<T>> work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(id, work, resiliencyLevel);
    public Task<T> Capture<T>(string id, Func<T> work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(id, work, resiliencyLevel);
    public Task Capture(string id, Func<Task> work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(id, work, resiliencyLevel);
    public Task Capture(string id, Action work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(id, work, resiliencyLevel);
}

public abstract class Flow : BaseFlow
{
    public abstract Task Run();
}

public abstract class Flow<TParam> : BaseFlow where TParam : notnull 
{
    public abstract Task Run(TParam param);
}

public abstract class Flow<TParam, TResult> : BaseFlow where TParam : notnull
{
    public abstract Task<TResult> Run(TParam param);
}