namespace Cleipnir.Flows.Sample.Presentation.Solutions.D_LoanApplication.Other;

public record LoanApplication(string Id, Guid CustomerId, decimal Amount, DateTime Created);