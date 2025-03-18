using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
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
    
    [TestMethod]
    public async Task FlowCanBeCreatedWithInitialState()
    {
        var flowsContainer = FlowsContainer.Create();
        var flow = new InitialStateFlow();
        var flows = flowsContainer.RegisterAnonymousFlow<InitialStateFlow, string, string>(
            flowFactory: () => flow
        );

        await flows.Run(
            "SomeInstanceId",
            param: "SomeParam",
            new InitialState(
                [new MessageAndIdempotencyKey("InitialMessageValue")],
                [new InitialEffect("InitialEffectId", "InitialEffectValue")]
            )
        );
        
        flow.InitialEffectValue.ShouldBe("InitialEffectValue");
        flow.InitialMessageValue.ShouldBe("InitialMessageValue");
    }

    private class InitialStateFlow : Flow<string, string>
    {
        public string? InitialEffectValue { get; set; }
        public string? InitialMessageValue { get; set; }
        
        public override async Task<string> Run(string _)
        {
            InitialEffectValue = await Effect.Get<string>("InitialEffectId");
            InitialMessageValue = await Messages.OfType<string>().First();
            return "";
        }
    }
}