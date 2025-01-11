using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.BankTransfer;

[GenerateFlows]
public class TransferFlow : Flow<Transfer>
{
    public override async Task Run(Transfer transfer)
    {
        var availableFunds = await _bankCentralClient.GetAvailableFunds(transfer.FromAccount);
        if (availableFunds <= transfer.Amount)
            throw new InvalidOperationException("Insufficient funds on from account");

        await _bankCentralClient.PostTransaction(
            transfer.TransactionId,
            transfer.FromAccount,
            -transfer.Amount
        );

        await _bankCentralClient.PostTransaction(
            transfer.TransactionId,
            transfer.ToAccount,
            transfer.Amount
        );
    }

    private Task<DistributedSemaphore.Lock> DistributedLock(string account)
        => Workflow.Synchronization.AcquireLock("BankTransfer", instance: account);

    private readonly IBankCentralClient _bankCentralClient = new BankCentralClient();
}