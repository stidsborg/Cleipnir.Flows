﻿using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.ConsoleApp.AtMostOnce;

[GenerateFlows]
public class AtMostOnceFlow : Flow<string>
{
    private readonly RocketSender _rocketSender = new();
    
    public override async Task Run(string rocketId)
    {
        await Effect.Capture(
            id: "FireRocket",
            _rocketSender.FireRocket,
            ResiliencyLevel.AtMostOnce
        );
    }
}