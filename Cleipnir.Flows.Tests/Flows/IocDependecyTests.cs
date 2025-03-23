using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class IocDependecyTests
{
    [TestMethod]
    public async Task DependentTypeCanHaveWorkflowInstanceInjected()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFlows(c => c
            .UseInMemoryStore()
            .RegisterFlow<TestFlow, TestFlows>()
        );
        serviceCollection.AddTransient<TestDependency>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var flows = serviceProvider.GetRequiredService<TestFlows>();
        var returned = await flows.Run("Instance", "Param");
        returned.ShouldBe("Instance@TestFlows");
    }

    private class TestFlows : Flows<TestFlow, string, string>
    {
        public TestFlows(FlowsContainer flowsContainer) 
            : base("TestFlows", flowsContainer, options: null, flowFactory: null) { }
    }
    
    private class TestFlow(TestDependency dependency) : Flow<string, string>
    {
        public override Task<string> Run(string param) 
            => dependency.Do().ToString().ToTask();
    }

    private class TestDependency(Workflow workflow)
    {
        public FlowId Do() => workflow.FlowId;
    }
}