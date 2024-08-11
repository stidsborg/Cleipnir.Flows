using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public interface IExposeState<TState> where TState : FlowState, new()
{
    public TState State { get; init; }
}