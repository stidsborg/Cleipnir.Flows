namespace Cleipnir.Flows.Sample.Console;

public static class Program
{
    private static async Task Main(string[] args)
    {
        await Middleware.Example.Do();
    }
}