namespace Cleipnir.Flows.Sample.Presentation.D_LoanApplication.Solution;

public record LoanApplication(string Id, Guid CustomerId, decimal Amount, DateTime Created);