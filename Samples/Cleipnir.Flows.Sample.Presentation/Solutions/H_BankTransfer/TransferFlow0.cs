using Cleipnir.Flows.Sample.Presentation.Solutions.H_BankTransfer.Other;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.H_BankTransfer;

[GenerateFlows]
public sealed class TransferFlow0 : Flow<Transfer>
{
    public TransferFlow0(IBankCentralClient bankCentralClient) => BankCentralClient = bankCentralClient;

    private IBankCentralClient BankCentralClient { get; }

    public override async Task Run(Transfer transfer)
    {
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