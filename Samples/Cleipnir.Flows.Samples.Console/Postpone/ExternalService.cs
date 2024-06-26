﻿using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows.Sample.ConsoleApp.Postpone;

public class ExternalService
{
    public Task<bool> IsOverloaded() => (Random.Shared.Next(0, 2) == 0).ToTask();
}