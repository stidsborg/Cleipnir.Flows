namespace Cleipnir.Flows.Sample.Console.AtMostOnce;

public class AtMostOnceFlow : Flow<string>
{
    private readonly RocketSender _rocketSender = new();
    
    public override async Task Run(string rocketId)
    {
        await DoAtMostOnce(
            workId: "FireRocket",
            _rocketSender.FireRocket
        );
    }
}