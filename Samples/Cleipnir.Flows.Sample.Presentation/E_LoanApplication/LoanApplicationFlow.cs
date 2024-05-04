using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.E_LoanApplication;

public class LoanApplicationFlow : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));
        
        //replies are of type CreditCheckOutcome
        
        var outcomes = await Messages
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .Completion();

        CommandAndEvents decision = DateTime.Now.Ticks % 2 == 0
            ? new LoanApplicationApproved(loanApplication)
            : new LoanApplicationRejected(loanApplication);

        await MessageBroker.Send(decision);
    }
}