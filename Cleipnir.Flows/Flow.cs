using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions;
using Cleipnir.ResilientFunctions.Messaging;

namespace Cleipnir.Flows;

//public abstract class Flow<TParam> : Flow<TParam, RScrapbook> {}
public abstract class Flow<TParam, TScrapbook> where TParam : notnull where TScrapbook : RScrapbook, new()
{
    public Context Context { get; init; } = null!;
    public Utilities Utilities => Context.Utilities;
    public Task<EventSource> EventSource => Context.EventSource;
    public TScrapbook Scrapbook { get; init; } = default!;

    public abstract Task Run(TParam param);
    
    public void Suspend(int whileEventCount) => throw new SuspendInvocationException(whileEventCount);
    public void Postpone(TimeSpan delay) => throw new PostponeInvocationException(delay);
    public void Postpone(DateTime until) => throw new PostponeInvocationException(until);
    
    public Task<string> DoAtMostOnce(string workId, Func<Task<string>> work) => Scrapbook.DoAtMostOnce(workId, work);
    public Task DoAtMostOnce(string workId, Func<Task> work) => Scrapbook.DoAtMostOnce(workId, work);
    public Task DoAtMostOnce(Expression<Func<TScrapbook, WorkStatus>> workStatus, Func<Task> work) 
        => Scrapbook.DoAtMostOnce(workStatus: workStatus, work);
    public Task DoAtMostOnce<TResult>(Expression<Func<TScrapbook, WorkStatusAndResult<TResult>>> workStatus, Func<Task<TResult>> work) =>
        Scrapbook.DoAtMostOnce(workStatus, work);

    public Task DoAtLeastOnce(string workId, Func<Task> work) => Scrapbook.DoAtLeastOnce(workId, work);
    public Task DoAtLeastOnce(Expression<Func<TScrapbook, WorkStatus>> workStatus,  Func<Task> work) => Scrapbook.DoAtLeastOnce(workStatus, work);
    public Task<string> DoAtLeastOnce(string workId, Func<Task<string>> work) => Scrapbook.DoAtLeastOnce(workId, work);
    public Task<TResult> DoAtLeastOnce<TResult>(Expression<Func<TScrapbook, WorkStatusAndResult<TResult>>> workStatus, Func<Task<TResult>> work)
        => Scrapbook.DoAtLeastOnce(workStatus, work);
}

public abstract class Flow<TParam, TScrapbook, TResult> where TParam : notnull where TScrapbook : RScrapbook, new()
{
    public Context Context { get; init; } = null!;
    public Utilities Utilities => Context.Utilities;
    public Task<EventSource> EventSource => Context.EventSource;
    public TScrapbook Scrapbook { get; init; } = default!;

    public abstract Task<TResult> Run(TParam param);
    
    public void Suspend(int whileEventCount) => throw new SuspendInvocationException(whileEventCount);
    public void Postpone(TimeSpan delay) => throw new PostponeInvocationException(delay);
    public void Postpone(DateTime until) => throw new PostponeInvocationException(until);
    
    public Task<string> DoAtMostOnce(string workId, Func<Task<string>> work) => Scrapbook.DoAtMostOnce(workId, work);
    public Task DoAtMostOnce(string workId, Func<Task> work) => Scrapbook.DoAtMostOnce(workId, work);
    public Task DoAtMostOnce(Expression<Func<TScrapbook, WorkStatus>> workStatus, Func<Task> work) 
        => Scrapbook.DoAtMostOnce(workStatus: workStatus, work);
    public Task DoAtMostOnce<TWorkResult>(Expression<Func<TScrapbook, WorkStatusAndResult<TWorkResult>>> workStatus, Func<Task<TWorkResult>> work) =>
        Scrapbook.DoAtMostOnce(workStatus, work);

    public Task DoAtLeastOnce(string workId, Func<Task> work) => Scrapbook.DoAtLeastOnce(workId, work);
    public Task DoAtLeastOnce(Expression<Func<TScrapbook, WorkStatus>> workStatus,  Func<Task> work) => Scrapbook.DoAtLeastOnce(workStatus, work);
    public Task<string> DoAtLeastOnce(string workId, Func<Task<string>> work) => Scrapbook.DoAtLeastOnce(workId, work);
    public Task<TWorkResult> DoAtLeastOnce<TWorkResult>(Expression<Func<TScrapbook, WorkStatusAndResult<TWorkResult>>> workStatus, Func<Task<TWorkResult>> work)
        => Scrapbook.DoAtLeastOnce(workStatus, work);
}