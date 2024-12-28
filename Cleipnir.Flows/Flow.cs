using System;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Reactive.Utilities;

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
    
    public Task<T> Capture<T>(Func<Task<T>> work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(work, resiliencyLevel);
    public Task<T> Capture<T>(Func<T> work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(work, resiliencyLevel);
    public Task Capture(Func<Task> work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(work, resiliencyLevel);
    public Task Capture(Action work, ResiliencyLevel resiliencyLevel = ResiliencyLevel.AtLeastOnce) 
        => Effect.Capture(work, resiliencyLevel);

    public void Postpone(TimeSpan @for) => Workflow.Postpone(@for);
    public void Postpone(DateTime until) => Workflow.Postpone(until);

    public Task<TMessage> Message<TMessage>() => Workflow.Messages.FirstOfType<TMessage>();
    public Task<Option<TMessage>> Message<TMessage>(string timeoutId, DateTime timesOutAt) => Workflow
            .Messages
            .TakeUntilTimeout(timeoutId, timesOutAt)
            .OfType<TMessage>()
            .FirstOrNone();
    public Task<Option<TMessage>> Message<TMessage>(string timeoutId, TimeSpan timesOutIn) => Workflow
            .Messages
            .TakeUntilTimeout(timeoutId, timesOutIn)
            .OfType<TMessage>()
            .FirstOrNone();
    public Task<Option<TMessage>> Message<TMessage>(DateTime timesOutAt) => Workflow
            .Messages
            .TakeUntilTimeout(timesOutAt)
            .OfType<TMessage>()
            .FirstOrNone();
    public Task<Option<TMessage>> Message<TMessage>(TimeSpan timesOutIn) => Workflow
            .Messages
            .TakeUntilTimeout(timesOutIn)
            .OfType<TMessage>()
            .FirstOrNone();
    
    public Task<Either<TMessage1, TMessage2>> Message<TMessage1, TMessage2>() 
        => Workflow.Messages.OfTypes<TMessage1, TMessage2>().First();
    public Task<EitherOrNone<TMessage1, TMessage2>> Message<TMessage1, TMessage2>(string timeoutId,
        DateTime timesOutAt) => Workflow
        .Messages
        .TakeUntilTimeout(timeoutId, timesOutAt)
        .OfTypes<TMessage1, TMessage2>()
        .FirstOrNone();
    public Task<EitherOrNone<TMessage1, TMessage2>> Message<TMessage1, TMessage2>(string timeoutId, TimeSpan timesOutIn) => Workflow
        .Messages
        .TakeUntilTimeout(timeoutId, timesOutIn)
        .OfTypes<TMessage1, TMessage2>()
        .FirstOrNone();
    public Task<EitherOrNone<TMessage1, TMessage2>> Message<TMessage1, TMessage2>(DateTime timesOutAt) => Workflow
        .Messages
        .TakeUntilTimeout(timesOutAt)
        .OfTypes<TMessage1, TMessage2>()
        .FirstOrNone();
    public Task<EitherOrNone<TMessage1, TMessage2>> Message<TMessage1, TMessage2>(TimeSpan timesOutIn) => Workflow
        .Messages
        .TakeUntilTimeout(timesOutIn)
        .OfTypes<TMessage1, TMessage2>()
        .FirstOrNone();
    public Task<Either<TMessage1, TMessage2, TMessage3>> Message<TMessage1, TMessage2, TMessage3>() 
        => Workflow.Messages.OfTypes<TMessage1, TMessage2, TMessage3>().First();
    public Task<EitherOrNone<TMessage1, TMessage2, TMessage3>> Message<TMessage1, TMessage2, TMessage3>(string timeoutId, DateTime timesOutAt) => Workflow
        .Messages
        .TakeUntilTimeout(timeoutId, timesOutAt)
        .OfTypes<TMessage1, TMessage2, TMessage3>()
        .FirstOrNone();
    public Task<EitherOrNone<TMessage1, TMessage2, TMessage3>> Message<TMessage1, TMessage2, TMessage3>(string timeoutId, TimeSpan timesOutIn) => Workflow
        .Messages
        .TakeUntilTimeout(timeoutId, timesOutIn)
        .OfTypes<TMessage1, TMessage2, TMessage3>()
        .FirstOrNone();
    public Task<EitherOrNone<TMessage1, TMessage2, TMessage3>> Message<TMessage1, TMessage2, TMessage3>(DateTime timesOutAt) => Workflow
        .Messages
        .TakeUntilTimeout(timesOutAt)
        .OfTypes<TMessage1, TMessage2, TMessage3>()
        .FirstOrNone();
    public Task<EitherOrNone<TMessage1, TMessage2, TMessage3>> Message<TMessage1, TMessage2, TMessage3>(TimeSpan timesOutIn) => Workflow
        .Messages
        .TakeUntilTimeout(timesOutIn)
        .OfTypes<TMessage1, TMessage2, TMessage3>()
        .FirstOrNone();

    public Task Delay(TimeSpan @for) => Messages
        .TakeUntilTimeout(@for)
        .Completion(maxWait: TimeSpan.Zero);
    public Task Delay(string timeoutId, TimeSpan @for) => Messages
        .TakeUntilTimeout(timeoutId, @for)
        .Completion(maxWait: TimeSpan.Zero);
    public Task Delay(DateTime until) => Messages
        .TakeUntilTimeout(until)
        .Completion(maxWait: TimeSpan.Zero);
    public Task Delay(string timeoutId, DateTime until) => Messages
        .TakeUntilTimeout(timeoutId, until)
        .Completion(maxWait: TimeSpan.Zero);
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