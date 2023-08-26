using Cleipnir.Flows.Reactive;

namespace Cleipnir.Flows.Sample.Presentation.C_LoanApplication;

public class LoanApplicationFlow : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));

        await EventSource.RegisterTimeout(timeoutId: "Timeout", expiresIn: TimeSpan.FromMinutes(15));
        
        //replies are of type CreditCheckOutcome
    }
}