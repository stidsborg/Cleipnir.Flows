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
                flowsFactory: sp => new OptionsTestWithOverriddenOptionsFlows(
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
        var store = new InMemoryFunctionStore();
        var storedType = await store.TypeStore.InsertOrGetStoredType("SomeOtherFlowName");
        
        serviceCollection.AddFlows(c => c
            .UseInMemoryStore(store)
            .WithOptions(new Options(messagesDefaultMaxWaitForCompletion: TimeSpan.MaxValue))
            .RegisterFlow<SimpleFlow, SimpleFlows>(
                flowsFactory: sp => new SimpleFlows(
                    sp.GetRequiredService<FlowsContainer>(),
                    flowName: "SomeOtherFlowName"
                )
            )
        );

        var sp = serviceCollection.BuildServiceProvider();
        var flows = sp.GetRequiredService<SimpleFlows>();
        await flows.Run("Id");
        var sf = await store.GetFunction(new StoredId(storedType, Instance: "Id".ToStoredInstance()));
        sf.ShouldNotBeNull();
        sf.Status.ShouldBe(Status.Succeeded);
    }
}

[GenerateFlows]
public class OptionsTestWithOverriddenOptionsFlow : Flow
{
    public override async Task Run()
    {
        await Messages.First();
    }
}
    
[GenerateFlows]
public class OptionsTestWithDefaultProvidedOptionsFlow : Flow
{
    public override async Task Run()
    {
        await Messages.First();
    }
}

[GenerateFlows]
public class SimpleFlow : Flow
{
    public override Task Run() => Task.CompletedTask;
}