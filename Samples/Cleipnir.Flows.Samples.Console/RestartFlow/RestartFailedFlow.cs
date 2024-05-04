namespace Cleipnir.Flows.Sample.ConsoleApp.RestartFlow;

public class RestartFailedFlow : Flow<string>
{
    public override async Task Run(string param)
    {
        if (param == "")
            throw new ArgumentException(nameof(param));

        await Task.CompletedTask;
    }
}