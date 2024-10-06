using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows.Tests.AspNet;

[GenerateFlows]
public class TestFlow : Flow<string, string>
{
    public override Task<string> Run(string param)
    {
        return param.ToUpper().ToTask();
    }
}