using Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication.Other;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication;

[GenerateFlows]
public class LoanApplicationFlow1 : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));

        var outcomes = new List<CreditCheckOutcome>();
        var messagesTask = Messages
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .Select(o =>
            {
                outcomes.Add(o);
                return o;
            })
            .Completion();

        await Task.WhenAny(
            messagesTask,
            Task.Delay(TimeSpan.FromMinutes(15))
        );
        
        CommandAndEvents decision = outcomes.All(o => o.Approved)
            ? new LoanApplicationApproved(loanApplication)
            : new LoanApplicationRejected(loanApplication);

        await MessageBroker.Send(decision);
    }
}