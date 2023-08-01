using System;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive;

namespace Cleipnir.Flows.Reactive;

public static class WorkExtensions
{
    public static async Task DoAtLeastOnce(this Task<EventSource> eventSource, string workId, Func<Task> work) 
        => await (await eventSource).DoAtLeastOnce(workId, work);

    public static async Task DoAtMostOnce(this Task<EventSource> eventSource, string workId, Func<Task> work)
        => await (await eventSource).DoAtMostOnce(workId, work);

    public static async Task<TResult> DoAtLeastOnce<TResult>(this Task<EventSource> eventSource, string workId, Func<Task<TResult>> work)
        => await (await eventSource).DoAtLeastOnce(workId, work);

    public static async Task<TResult> DoAtMostOnce<TResult>(this Task<EventSource> eventSource, string workId, Func<Task<TResult>> work)
        => await (await eventSource).DoAtMostOnce(workId, work);
}