namespace Cleipnir.Flows.Sample.Presentation.E_LoanApplication;

public record LoanApplication(string Id, Guid CustomerId, decimal Amount, DateTime Created);