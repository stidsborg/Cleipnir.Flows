namespace Cleipnir.Flows.Sample.Presentation.C_LoanApplication;

public record LoanApplication(string Id, Guid CustomerId, decimal Amount, DateTime Created);