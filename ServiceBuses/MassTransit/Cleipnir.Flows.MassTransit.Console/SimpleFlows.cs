namespace Cleipnir.Flows.MassTransit.Console;

public class SimpleFlows : Flows<SimpleFlow>
{
    public SimpleFlows(FlowsContainer flowsContainer) 
        : base("SimpleFlow", flowsContainer, options: null) { }
}