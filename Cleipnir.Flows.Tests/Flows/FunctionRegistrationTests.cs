using Cleipnir.ResilientFunctions.Helpers;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class FunctionRegistrationTests
{
    [TestMethod]
    public async Task FunctionCanBeRegisteredAndInvoked()
    {
        using var container = FlowsContainer.Create();
        var registration = container.Functions.RegisterFunc(
            "TestFlow",
            inner: Task<string> (string param) => param.ToUpper().ToTask()
        );

        var returned = await registration.Invoke("SomeInstance", "hallo world");
        returned.ShouldBe("HALLO WORLD");
    }
}