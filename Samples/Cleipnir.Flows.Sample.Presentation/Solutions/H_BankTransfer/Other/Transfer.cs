namespace Cleipnir.Flows.Sample.Presentation.Solutions.H_BankTransfer.Other;

public record Transfer(
    Guid TransactionId,
    string FromAccount,
    string ToAccount,
    decimal Amount
);