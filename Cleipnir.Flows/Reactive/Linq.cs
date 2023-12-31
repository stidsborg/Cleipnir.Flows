﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive;

namespace Cleipnir.Flows.Reactive;

public static class Linq
{
    #region EventSource properties

    public static async Task RegisterTimeout(this Task<EventSource> eventSource, string timeoutId, TimeSpan expiresIn) 
        => await (await eventSource).RegisterTimeoutEvent(timeoutId, expiresIn);
    
    public static async Task RegisterTimeout(this Task<EventSource> eventSource, string timeoutId, DateTime expiresAt) 
        => await (await eventSource).RegisterTimeoutEvent(timeoutId, expiresAt);
    
    public static async Task CancelTimeout(this Task<EventSource> eventSource, string timeoutId) 
        => await (await eventSource).CancelTimeoutEvent(timeoutId);

    #endregion
    #region Non leaf operators

    public static async Task<IReactiveChain<TOut>> Select<TIn, TOut>(this Task<IReactiveChain<TIn>> s, Func<TIn, TOut> mapper)
        => (await s).Select(mapper);

    public static async Task<IReactiveChain<TFolded>> Scan<T, TFolded>(this Task<IReactiveChain<T>> s, TFolded seed, Func<TFolded, T, TFolded> folder)
        => (await s).Scan(seed, folder);

    public static async Task<IReactiveChain<T>> Where<T>(this Task<IReactiveChain<T>> s, Func<T, bool> filter)
        => (await s).Where(filter);
    public static async Task<IReactiveChain<T>> OfType<T>(this Task<IReactiveChain<object>> s)
        => (await s).OfType<T>();

    public static async Task<IReactiveChain<Either<T1, T2>>> OfTypes<T1, T2>(this Task<IReactiveChain<object>> s)
        => (await s).OfTypes<T1, T2>();

    public static async Task<IReactiveChain<Either<T1, T2, T3>>> OfTypes<T1, T2, T3>(this Task<IReactiveChain<object>> s)
        => (await s).OfTypes<T1, T2, T3>();

    public static async Task<IReactiveChain<T>> Take<T>(this Task<IReactiveChain<T>> s, int toTake)
        => (await s).Take(toTake);

    public static async Task<IReactiveChain<T>> TakeUntil<T>(this Task<IReactiveChain<T>> s, Func<T, bool> predicate)
        => (await s).TakeUntil(predicate);

    public static async Task<IReactiveChain<T>> Skip<T>(this Task<IReactiveChain<T>> s, int toSkip)
        => (await s).Skip(toSkip);

    public static async Task<IReactiveChain<List<T>>> Buffer<T>(this Task<IReactiveChain<T>> s, int bufferSize)
        => (await s).Buffer(bufferSize);

    public static async Task<IReactiveChain<List<T>>> Chunk<T>(this Task<IReactiveChain<T>> s, int size)
        => (await s).Chunk(size);

    public static async Task<IReactiveChain<T>> Merge<T>(this Task<IReactiveChain<T>> stream1, Task<IReactiveChain<T>> stream2)
        => ResilientFunctions.Reactive.Linq.Merge(await stream1, await stream2);
    
    // ** EventSource extensions ** //
    public static async Task<IReactiveChain<TOut>> Select<TOut>(this Task<EventSource> s, Func<object, TOut> mapper)
        => (await s).Select(mapper);

    public static async Task<IReactiveChain<TFolded>> Scan<TFolded>(this Task<EventSource> s, TFolded seed, Func<TFolded, object, TFolded> folder)
        => (await s).Scan(seed, folder);

    public static async Task<IReactiveChain<object>> Where(this Task<EventSource> s, Func<object, bool> filter)
        => (await s).Where(filter);
    public static async Task<IReactiveChain<T>> OfType<T>(this Task<EventSource> s)
        => (await s).OfType<T>();

    public static async Task<IReactiveChain<Either<T1, T2>>> OfTypes<T1, T2>(this Task<EventSource> s)
        => (await s).OfTypes<T1, T2>();

    public static async Task<IReactiveChain<Either<T1, T2, T3>>> OfTypes<T1, T2, T3>(this Task<EventSource> s)
        => (await s).OfTypes<T1, T2, T3>();

    public static async Task<IReactiveChain<object>> Take(this Task<EventSource> s, int toTake)
        => (await s).Take(toTake);

    public static async Task<IReactiveChain<object>> TakeUntil(this Task<EventSource> s, Func<object, bool> predicate)
        => (await s).TakeUntil(predicate);

    public static async Task<IReactiveChain<object>> Skip(this Task<EventSource> s, int toSkip)
        => (await s).Skip(toSkip);

    public static async Task<IReactiveChain<List<object>>> Buffer(this Task<EventSource> s, int bufferSize)
        => (await s).Buffer(bufferSize);

    public static async Task<IReactiveChain<List<object>>> Chunk(this Task<EventSource> s, int size)
        => (await s).Chunk(size);
    
    #endregion

    #region Leaf operators

    public static async Task<T> Last<T>(this Task<IReactiveChain<T>> s)
        => await (await s).Last();

    public static async Task<List<T>> ToList<T>(this Task<IReactiveChain<T>> s)
        => await (await s).ToList();

    public static async Task<List<T>> PullExisting<T>(this Task<IReactiveChain<T>> s)
        => (await s).PullExisting();
    
    // ** NEXT RELATED OPERATORS ** //
    public static async Task<T> Next<T>(this Task<IReactiveChain<T>> s) 
        => await (await s).Next();
    public static async Task<T> Next<T>(this Task<IReactiveChain<T>> s, int maxWaitMs)
        => await (await s).Next(maxWaitMs);
    public static async Task<T> Next<T>(this Task<IReactiveChain<T>> s, TimeSpan maxWait)
        => await (await s).Next(maxWait);

    public static async Task<Option<T>> TryNext<T>(this Task<IReactiveChain<T>> s)
    {
        var success = (await s).TryNext(out var value, out var totalEventSourceCount);
        return new Option<T>(success, value, totalEventSourceCount);
    }

    public static async Task<T> NextOfType<T>(this Task<IReactiveChain<object>> s)
        => await (await s).NextOfType<T>();
    public static async Task<T> NextOfType<T>(this Task<IReactiveChain<object>> s, TimeSpan maxWait)
        => await (await s).NextOfType<T>();

    public static async Task<Option<T>> TryNextOfType<T>(this Task<IReactiveChain<object>> s)
    {
        var success = (await s).TryNextOfType<T>(out var next, out var totalEventSourceCount);
        return new Option<T>(success, next, totalEventSourceCount);
    }

    public static async Task<T> SuspendUntilNext<T>(this Task<IReactiveChain<T>> s, TimeSpan waitBeforeSuspension)
        => await (await s).SuspendUntilNext(waitBeforeSuspension);
    public static async Task<T> SuspendUntilNext<T>(this Task<IReactiveChain<T>> s)
        => await (await s).SuspendUntilNext();
    public static async Task<T> SuspendUntilNextOfType<T>(this Task<IReactiveChain<object>> s)
        => await (await s).SuspendUntilNextOfType<T>();
    public static async Task<T> SuspendUntilNextOfType<T>(this Task<IReactiveChain<object>> s, TimeSpan waitBeforeSuspension)
        => await (await s).SuspendUntilNextOfType<T>(waitBeforeSuspension);
    public static async Task<TimeoutOption<T>> SuspendUntilNext<T>(this Task<IReactiveChain<T>> s, string timeoutEventId, TimeSpan expiresIn)
        => await (await s).SuspendUntilNext(timeoutEventId, expiresIn);
    public static async Task<TimeoutOption<T>> SuspendUntilNext<T>(this Task<IReactiveChain<T>> s, string timeoutEventId, DateTime expiresAt)
        => await (await s).SuspendUntilNext<T>(timeoutEventId, expiresAt);
    
    // ** EventSource extensions ** //
    public static async Task SuspendUntil(this Task<EventSource> s, string timeoutId, DateTime resumeAt)
        => await (await s).SuspendUntil(timeoutId, resumeAt);
    
    public static async Task SuspendFor(this Task<EventSource> s, string timeoutId, TimeSpan resumeAfter)
        => await (await s).SuspendFor(timeoutId, resumeAfter);
    
    public static async Task<List<object>> PullExisting(this Task<EventSource> s)
        => (await s).PullExisting();
    
    // ** NEXT RELATED OPERATORS ** //
    public static async Task<object> Next(this Task<EventSource> s) 
        => await (await s).Next();
    public static async Task<object> Next(this Task<EventSource> s, int maxWaitMs)
        => await (await s).Next(maxWaitMs);
    public static async Task<object> Next(this Task<EventSource> s, TimeSpan maxWait)
        => await (await s).Next(maxWait);

    public static async Task<Option<object?>> TryNext(this Task<EventSource> s)
    {
        var success = (await s).TryNext(out var value, out var totalEventSourceCount);
        return new Option<object?>(success, value, totalEventSourceCount);
    }

    public static async Task<T> NextOfType<T>(this Task<EventSource> s)
        => await (await s).NextOfType<T>();
    public static async Task<T> NextOfType<T>(this Task<EventSource> s, TimeSpan maxWait)
        => await (await s).NextOfType<T>();

    public static async Task<Option<T>> TryNextOfType<T>(this Task<EventSource> s)
    {
        var success = (await s).TryNextOfType<T>(out var next, out var totalEventSourceCount);
        return new Option<T>(success, next, totalEventSourceCount);
    }

    public static async Task<object> SuspendUntilNext(this Task<EventSource> s, TimeSpan waitBeforeSuspension)
        => await (await s).SuspendUntilNext(waitBeforeSuspension);
    public static async Task<object> SuspendUntilNext(this Task<EventSource> s)
        => await (await s).SuspendUntilNext();
    public static async Task<T> SuspendUntilNextOfType<T>(this Task<EventSource> s)
        => await (await s).SuspendUntilNextOfType<T>();
    public static async Task<T> SuspendUntilNextOfType<T>(this Task<EventSource> s, TimeSpan waitBeforeSuspension)
        => await (await s).SuspendUntilNextOfType<T>(waitBeforeSuspension);
    public static async Task<object> SuspendUntilNext(this Task<EventSource> s, string timeoutEventId, TimeSpan expiresIn)
        => await (await s).SuspendUntilNext(timeoutEventId, expiresIn);
    public static async Task<object> SuspendUntilNext(this Task<EventSource> s, string timeoutId, DateTime expiresAt)
        => await (await s).SuspendUntilNext(timeoutId, expiresAt);

    public record struct Option<T>(bool HasValue, T? Value, int TotalEventSourceCount);

    #endregion
}