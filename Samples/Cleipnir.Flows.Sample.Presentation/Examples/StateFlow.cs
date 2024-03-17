using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Presentation.Examples;

public class StateFlow : Flow<string>
{
    public override async Task Run(string param)
    {
        var state = await Effect.CreateOrGet<State>("State");
        if (state.Started == null)
        {
            state.Started = DateTime.Now;
            await state.Save();
        }

        Console.WriteLine("Flow was initially started: " + state.Started);
    }

    public class State : WorkflowState
    {
        public DateTime? Started { get; set; }
    }
}