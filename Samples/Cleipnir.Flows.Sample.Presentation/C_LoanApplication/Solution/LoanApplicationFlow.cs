﻿using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.C_LoanApplication.Solution;

public class LoanApplicationFlow : Flow<LoanApplication>
{
    public override async Task Run(LoanApplication loanApplication)
    {
        await MessageBroker.Send(new PerformCreditCheck(loanApplication.Id, loanApplication.CustomerId, loanApplication.Amount));

        await Messages.TimeoutProvider.RegisterTimeout(timeoutId: "Timeout", expiresIn: TimeSpan.FromMinutes(15));

        var outcomesAndTimeout = await Messages
            .Chunk(3)
            .SuspendUntilFirst();

        var outcomes = outcomesAndTimeout
            .TakeWhile(e => e is CreditCheckOutcome)
            .OfType<CreditCheckOutcome>()
            .ToList();

        if (outcomes.Count < 2)
            await MessageBroker.Send(new LoanApplicationRejected(loanApplication));
        else
            await MessageBroker.Send(
                outcomes.All(o => o.Approved)
                    ? new LoanApplicationApproved(loanApplication)
                    : new LoanApplicationRejected(loanApplication)
            );
    }
}