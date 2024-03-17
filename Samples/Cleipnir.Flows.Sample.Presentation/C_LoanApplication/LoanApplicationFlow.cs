using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.C_LoanApplication;

public class LoanApplicationFlow : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));

        //replies are of type CreditCheckOutcome
        
        var outcomes = await Messages
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .SuspendUntilCompletion();
    }
}