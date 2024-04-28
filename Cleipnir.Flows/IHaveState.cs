using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public interface IHaveState<TState> where TState : WorkflowState, new()
{
    public TState State { get; init; }
}