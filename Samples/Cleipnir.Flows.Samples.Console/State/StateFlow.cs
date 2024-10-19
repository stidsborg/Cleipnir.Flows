using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.ConsoleApp.State;

[GenerateFlows]
public class StateFlow : Flow<string, string>
{
    public required WorkflowState State { get; init; }
    
    public override async Task<string> Run(string param)
    {
        if (State.Started == null)
        {
            State.Started = DateTime.Now;
            await State.Save();
        }
        
        Console.WriteLine("Flow was initially started: " + State.Started);
        
        return param;
    }

    public class WorkflowState : FlowState
    {
        public DateTime? Started { get; set; }
    }
}