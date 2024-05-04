namespace Cleipnir.Flows.Sample.Presentation.Z_BankTransfer;

public record Transfer(
    Guid TransactionId,
    string FromAccount,
    string ToAccount,
    decimal Amount
);