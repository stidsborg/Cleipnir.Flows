using Cleipnir.Flows.Sample.Presentation.Solutions.H_BankTransfer.Other;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.H_BankTransfer;

public sealed class TransferFlow : Flow<Transfer>
{
    public TransferFlow(IBankCentralClient bankCentralClient) => BankCentralClient = bankCentralClient;

    private IBankCentralClient BankCentralClient { get; }

    public override async Task Run(Transfer transfer)
    {
        await using var @lock = await Utilities.Monitor.Acquire(
            group: "BankTransfer", name: transfer.FromAccount, lockId: transfer.TransactionId.ToString()
        );
        
        var availableFunds = await BankCentralClient.GetAvailableFunds(transfer.FromAccount);
        if (availableFunds <= transfer.Amount)
            throw new InvalidOperationException("Insufficient funds on from account");
        
        await BankCentralClient.PostTransaction(
            transfer.TransactionId,
            transfer.FromAccount,
            -transfer.Amount
        );

        await BankCentralClient.PostTransaction(
            transfer.TransactionId,
            transfer.ToAccount,
            transfer.Amount
        );
    }
}