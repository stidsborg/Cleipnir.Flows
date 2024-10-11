using System;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive.Utilities;

namespace Cleipnir.Flows.Helpers;

internal static class TaskExtensions
{
    public static async Task<T> ThrowTimeoutExceptionOnNoResult<T>(this Task<Option<T>> task)
    {
        var option = await task;
        if (option.HasValue)
            return option.Value;

        throw new TimeoutException();
    }
    
    public static async Task<Either<T1, T2>> ThrowTimeoutExceptionOnNoResult<T1, T2>(this Task<EitherOrNone<T1, T2>> task)
    {
        var eitherOrNone = await task;
        return eitherOrNone.Match(
            first: Either<T1, T2>.CreateFirst,
            second: Either<T1, T2>.CreateSecond,
            none: () => throw new TimeoutException()
        );
    }
    
    public static async Task<Either<T1, T2, T3>> ThrowTimeoutExceptionOnNoResult<T1, T2, T3>(this Task<EitherOrNone<T1, T2, T3>> task)
    {
        var eitherOrNone = await task;
        return eitherOrNone.Match(
            first: Either<T1, T2, T3>.CreateFirst,
            second: Either<T1, T2, T3>.CreateSecond,
            third: Either<T1, T2, T3>.CreateThird,
            none: () => throw new TimeoutException()
        );
    }
}