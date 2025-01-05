using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.BankTransfer;

[GenerateFlows]
public class TransferFlow(IBankCentralClient bankCentralClient) : Flow<Transfer>
{
    public override async Task Run(Transfer transfer)
    {
        var availableFunds = await bankCentralClient.GetAvailableFunds(transfer.FromAccount);
        if (availableFunds <= transfer.Amount)
            throw new InvalidOperationException("Insufficient funds on from account");

        await bankCentralClient.PostTransaction(
            transfer.TransactionId,
            transfer.FromAccount,
            -transfer.Amount
        );

        await bankCentralClient.PostTransaction(
            transfer.TransactionId,
            transfer.ToAccount,
            transfer.Amount
        );
    }

    private DistributedSemaphore DistributedLock(string account)
        => Workflow.Semaphores.Create("BankTransfer", account, maximumCount: 1);
}