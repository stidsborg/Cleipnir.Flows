﻿using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.D_LoanApplication;

[GenerateFlows]
public class LoanApplicationFlow : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await Bus.Publish(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));
        
        //replies are of type CreditCheckOutcome
        
        var outcomes = await Messages
            .OfType<CreditCheckOutcome>()
            .Take(3)
            .Completion();

        CommandAndEvents decision = DateTime.Now.Ticks % 2 == 0
            ? new LoanApplicationApproved(loanApplication)
            : new LoanApplicationRejected(loanApplication);

        await Bus.Publish(decision);
    }
}