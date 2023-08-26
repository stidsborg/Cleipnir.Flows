namespace Cleipnir.Flows.Sample.Presentation.C_LoanApplication;

public record CommandAndEvents;
public record PerformCreditCheck(string Id, Guid CustomerId, decimal Amount) : CommandAndEvents;
public record CreditCheckOutcome(string CreditChecker, string LoanApplicationId, bool Approved) : CommandAndEvents;
public record LoanApplicationApproved(LoanApplication LoanApplication) : CommandAndEvents;
public record LoanApplicationRejected(LoanApplication LoanApplication) : CommandAndEvents;

