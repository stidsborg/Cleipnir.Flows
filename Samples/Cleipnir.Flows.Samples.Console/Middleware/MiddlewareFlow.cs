namespace Cleipnir.Flows.Sample.Console.Middleware;

public class MiddlewareFlow : Flow<string>
{
    public override Task Run(string param)
    {
        var randomValue = Random.Shared.Next(0, 3);
        if (randomValue == 1)
            throw new TimeoutException();
        if (randomValue == 2)
            Postpone(delay: TimeSpan.Zero);

        return Task.CompletedTask;
    }
}