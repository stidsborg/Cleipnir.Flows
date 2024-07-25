using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.D_LoanApplication.Solution;

public class LoanApplicationFlow : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await Bus.Publish(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));
        
        var outcomes = await Messages
            .TakeUntilTimeout("Timeout", TimeSpan.FromMinutes(15))
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .Completion();
        
        if (outcomes.Count < 2)
            await Bus.Publish(new LoanApplicationRejected(loanApplication));
        else
            await Bus.Publish(
                outcomes.All(o => o.Approved)
                    ? new LoanApplicationApproved(loanApplication)
                    : new LoanApplicationRejected(loanApplication)
            );
    }
}