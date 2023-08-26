namespace Cleipnir.Flows.Sample.Presentation.C_LoanApplication.Solution;

public record LoanApplication(string Id, Guid CustomerId, decimal Amount, DateTime Created);