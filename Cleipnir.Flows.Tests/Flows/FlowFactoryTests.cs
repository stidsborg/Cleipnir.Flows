using Cleipnir.ResilientFunctions.Helpers;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class FlowFactoryTests
{
    [TestMethod]
    public async Task FlowFactoryForTestFlowWithResultIsUsedWhenProvided()
    {
        var flowsContainer = FlowsContainer.Create();
        var flows = flowsContainer.RegisterAnonymousFlow<TestFlowWithResult, string, string>(flowFactory: () => new TestFlowWithResult());
        var result = await flows.Run("SomeInstance", "hallo world");
        result.ShouldBe("HALLO WORLD");
        TestFlowWithResult.Invoked.ShouldBeTrue();
    }

    private class TestFlowWithResult : Flow<string, string>
    {
        public static volatile bool Invoked = false;

        public override Task<string> Run(string param)
        {
            Invoked = true;
            return param.ToUpper().ToTask();   
        }
    }
    
    [TestMethod]
    public async Task FlowFactoryForTestFlowWithParamIsUsedWhenProvided()
    {
        var flowsContainer = FlowsContainer.Create();
        var flows = flowsContainer.RegisterAnonymousFlow<TestFlowWithParam, string>(flowFactory: () => new TestFlowWithParam());
        await flows.Run("SomeInstance", "hallo world");
        
        TestFlowWithParam.Invoked.ShouldBeTrue();
        TestFlowWithParam.Param.ShouldBe("hallo world");
    }
    
    private class TestFlowWithParam : Flow<string>
    {
        public static volatile bool Invoked = false;
        public static string? Param = null;

        public override Task Run(string param)
        {
            Param = param;
            Invoked = true;
            
            return Task.CompletedTask;
        }
    }
    
    [TestMethod]
    public async Task FlowFactoryForTestFlowWithoutParamIsUsedWhenProvided()
    {
        var flowsContainer = FlowsContainer.Create();
        var flows = flowsContainer.RegisterAnonymousFlow(flowFactory: () => new TestFlowWithoutParam());
        await flows.Run("SomeInstance");
        
        TestFlowWithoutParam.Invoked.ShouldBeTrue();
    }
    
    private class TestFlowWithoutParam : Flow
    {
        public static volatile bool Invoked = false;

        public override Task Run()
        {
            Invoked = true;
            return Task.CompletedTask;
        }
    }
}