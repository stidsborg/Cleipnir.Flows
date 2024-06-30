using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class FlowsWithMiddlewareTests
{
    [TestMethod]
    public async Task SimpleFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<SimpleMiddlewareFlow>();
        serviceCollection.AddSingleton<SimpleMiddleware>();
        
        var flowStore = new InMemoryFunctionStore();
        var options = new Options();
        options.UseMiddleware<SimpleMiddleware>();
        using var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            options
        );

        var flows = new SimpleMiddlewareFlows(flowsContainer);

        await Should.ThrowAsync<TimeoutException>(
            flows.Run("someInstanceId", "someParameter")
        );

        SimpleMiddleware.Param.ShouldBe("someParameter");
        var result = (Result<int>?)SimpleMiddleware.Result;
        result.ShouldNotBeNull();
        result.SucceedWithValue.ShouldBe(1);
    }
    
    public class SimpleMiddlewareFlow : Flow<string, int>
    {
        public override Task<int> Run(string param) => 1.ToTask();
    }
    
    private class SimpleMiddleware : IMiddleware
    {
        public static object? Param = null;
        public static object? Result = null;
        
        public async Task<Result<TResult>> Run<TFlow, TParam, TResult>(
            TParam param, 
            Workflow workflow, 
            Next<TFlow, TParam, TResult> next
        ) where TParam : notnull
        {
            Param = param;
            var result = await next(param, workflow);
            Result = result;

            return new Result<TResult>(failWith: new TimeoutException());
        }
    }
}