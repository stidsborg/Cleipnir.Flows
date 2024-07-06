using Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication.Other;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication;

public class LoanApplicationFlow2 : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(PerformCreditCheck(loanApplication));
        
        var outcomes = await Messages
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .TakeUntilTimeout("Timeout", TimeSpan.FromMinutes(15))
            .Completion();
        
        if (outcomes.Count < 2)
            await MessageBroker.Send(LoanApplicationRejected(loanApplication));
        else
            await MessageBroker.Send(
                outcomes.All(o => o.Approved)
                    ? LoanApplicationApproved(loanApplication)
                    : LoanApplicationRejected(loanApplication)
            );
    }

    private static PerformCreditCheck PerformCreditCheck(LoanApplication loanApplication)
        => new(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount);
    private static LoanApplicationRejected LoanApplicationRejected(LoanApplication loanApplication) 
        => new(loanApplication);
    private static LoanApplicationApproved LoanApplicationApproved(LoanApplication loanApplication) 
        => new(loanApplication);
}