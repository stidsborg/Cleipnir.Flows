using Cleipnir.ResilientFunctions.Domain.Exceptions.Commands;

namespace Cleipnir.Flows.Sample.ConsoleApp.Middleware;

public class MiddlewareFlow : Flow<string>
{
    public override Task Run(string param)
    {
        var randomValue = Random.Shared.Next(0, 3);
        if (randomValue == 1)
            throw new TimeoutException();
        if (randomValue == 2)
            throw new PostponeInvocationException(postponeForMs: 0);

        return Task.CompletedTask;
    }
}