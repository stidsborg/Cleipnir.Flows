using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Domain.Exceptions;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cleipnir.Flows.Tests.Flows;

[TestClass]
public class OptionsTests
{
    [TestMethod]
    public async Task SimpleFlowCompletesSuccessfully()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddFlows(c => c
            .UseInMemoryStore()
            .WithOptions(new Options(messagesDefaultMaxWaitForCompletion: TimeSpan.MaxValue))
            .RegisterFlow<OptionsTestWithOverriddenOptionsFlow, OptionsTestWithOverriddenOptionsFlows>(
                factory: sp => new OptionsTestWithOverriddenOptionsFlows(
                    sp.GetRequiredService<FlowsContainer>(),
                    options: new Options(messagesDefaultMaxWaitForCompletion: TimeSpan.Zero)
                )
            )
            .RegisterFlow<OptionsTestWithDefaultProvidedOptionsFlow, OptionsTestWithDefaultProvidedOptionsFlows>()
        );

        var sp = serviceCollection.BuildServiceProvider();
        var flowsWithOverridenOptions = sp.GetRequiredService<OptionsTestWithOverriddenOptionsFlows>();

        await Should.ThrowAsync<InvocationSuspendedException>(
            () => flowsWithOverridenOptions.Run("Id")
        );
        
        var flowsWithDefaultProvidedOptions = sp.GetRequiredService<OptionsTestWithDefaultProvidedOptionsFlows>();
        await flowsWithDefaultProvidedOptions.Schedule("Id");

        await Task.Delay(100);
        
        var controlPanel = await flowsWithDefaultProvidedOptions.ControlPanel("Id");
        controlPanel.ShouldNotBeNull();
        controlPanel.Status.ShouldBe(Status.Executing);

        await controlPanel.Messages.Append("Hello");

        await controlPanel.WaitForCompletion();        
    }

   
    
    [TestMethod]
    public async Task FlowNameCanBeSpecifiedFromTheOutside()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddFlows(c => c
            .UseInMemoryStore()
            .WithOptions(new Options(messagesDefaultMaxWaitForCompletion: TimeSpan.MaxValue))
            .RegisterFlow<SimpleFlow, SimpleFlows>(
                factory: sp => new SimpleFlows(
                    sp.GetRequiredService<FlowsContainer>(),
                    flowName: "SomeOtherFlowName"
                )
            )
        );

        var sp = serviceCollection.BuildServiceProvider();
        var flows = sp.GetRequiredService<SimpleFlows>();
        await flows.Run("Id");
        var store = sp.GetRequiredService<IFunctionStore>();
        var sf = await store.GetFunction(new FlowId("SomeOtherFlowName", "Id"));
        sf.ShouldNotBeNull();
        sf.Status.ShouldBe(Status.Succeeded);
    }
}

public class OptionsTestWithOverriddenOptionsFlow : Flow
{
    public override async Task Run()
    {
        await Messages.First();
    }
}
    
public class OptionsTestWithDefaultProvidedOptionsFlow : Flow
{
    public override async Task Run()
    {
        await Messages.First();
    }
}

public class SimpleFlow : Flow
{
    public override Task Run() => Task.CompletedTask;
}