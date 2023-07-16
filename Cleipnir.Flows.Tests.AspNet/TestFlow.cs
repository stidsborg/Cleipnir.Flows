using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows.Tests.AspNet;

public class TestFlow : Flow<string, RScrapbook, string>
{
    public override Task<string> Run(string param)
    {
        return param.ToUpper().ToTask();
    }
}