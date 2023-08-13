using Cleipnir.ResilientFunctions.CoreRuntime;
using Cleipnir.ResilientFunctions.Domain.Events;

namespace Cleipnir.Flows.Tests.Reactive;

public class NoOpTimeoutProvider : ITimeoutProvider
{
    public static NoOpTimeoutProvider Instance { get; } = new();
    public Task RegisterTimeout(string timeoutId, DateTime expiresIn, bool overwrite = false)
        => Task.CompletedTask;
    public Task RegisterTimeout(string timeoutId, TimeSpan expiresIn, bool overwrite = false)
        => Task.CompletedTask;
    public Task CancelTimeout(string timeoutId) => Task.CompletedTask;
    public Task<List<TimeoutEvent>> PendingTimeouts() => Task.FromResult(new List<TimeoutEvent>());
}