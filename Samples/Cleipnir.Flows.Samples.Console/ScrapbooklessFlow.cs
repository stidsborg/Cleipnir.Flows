namespace Cleipnir.Flows.Sample.ConsoleApp;

public class ScrapbooklessFlow : Flow<string>
{
    public override Task<string> Run(string param)
    {
        System.Console.WriteLine("Executing: " + nameof(ScrapbooklessFlow));
        return Task.FromResult<string>("hello world");
    }
}