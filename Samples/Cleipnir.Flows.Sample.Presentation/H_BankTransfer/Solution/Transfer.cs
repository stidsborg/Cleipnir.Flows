namespace Cleipnir.Flows.Sample.Presentation.H_BankTransfer.Solution;

public record Transfer(
    Guid TransactionId,
    string FromAccount,
    string ToAccount,
    decimal Amount
);