using Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication.Other;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication;

public class LoanApplicationFlow0 : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));
        
        var outcomes = await Messages
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .Completion();

        CommandAndEvents decision = outcomes.All(o => o.Approved)
            ? new LoanApplicationApproved(loanApplication)
            : new LoanApplicationRejected(loanApplication);

        await MessageBroker.Send(decision);
    }
}