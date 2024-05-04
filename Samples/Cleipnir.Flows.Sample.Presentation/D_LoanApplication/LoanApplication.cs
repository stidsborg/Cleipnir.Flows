namespace Cleipnir.Flows.Sample.Presentation.D_LoanApplication;

public record LoanApplication(string Id, Guid CustomerId, decimal Amount, DateTime Created);