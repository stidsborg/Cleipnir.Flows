namespace Cleipnir.Flows.Sample.ConsoleApp;

public class BookingFlow : Flow<string, string>
{
    public override Task<string> Run(string @param)
    {
        System.Console.WriteLine("ok");
        return Task.FromResult<string>("hello world");
    }
}