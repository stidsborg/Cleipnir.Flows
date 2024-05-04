using Cleipnir.ResilientFunctions.Domain.Exceptions;

namespace Cleipnir.Flows.Sample.ConsoleApp.Postpone;

public class PostponeFlow : Flow<string>
{
    private readonly ExternalService _externalService = new();
    
    public override async Task Run(string orderId)
    {
        if (await _externalService.IsOverloaded())
            throw new PostponeInvocationException(postponeFor: TimeSpan.FromMinutes(10));
        
        //execute rest of the flow
    }
}