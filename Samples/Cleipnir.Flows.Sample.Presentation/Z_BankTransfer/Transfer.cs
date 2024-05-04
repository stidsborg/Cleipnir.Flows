namespace Cleipnir.Flows.Sample.Presentation.B_BankTransfer;

public record Transfer(
    Guid TransactionId,
    string FromAccount,
    string ToAccount,
    decimal Amount
);