using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Console;

public class BookingFlow : Flow<string, RScrapbook, string>
{
    public override Task<string> Run(string @param)
    {
        System.Console.WriteLine("ok");
        return Task.FromResult<string>("hello world");
    }
}