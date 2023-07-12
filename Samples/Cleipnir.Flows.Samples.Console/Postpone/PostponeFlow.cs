namespace Cleipnir.Flows.Sample.Console.Postpone;

public class PostponeFlow : Flow<string>
{
    private readonly ExternalService _externalService = new();
    
    public override async Task Run(string orderId)
    {
        if (await _externalService.IsOverloaded())
            Postpone(delay: TimeSpan.FromMinutes(10));
        
        //execute rest of the flow
    }
}