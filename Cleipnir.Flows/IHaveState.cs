using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public interface IHaveState<TState> where TState : FlowState, new()
{
    public TState State { get; init; }
}