namespace Cleipnir.Flows.Sample.Presentation.H_BankTransfer;

[GenerateFlows]
public sealed class TransferFlow : Flow<Transfer>
{
    public TransferFlow(IBankCentralClient bankCentralClient) => BankCentralClient = bankCentralClient;

    private IBankCentralClient BankCentralClient { get; }

    public override async Task Run(Transfer transfer)
    {
        var (transactionId, fromAccount, toAccount, amount) = transfer;
        
        //check available funds
        
        //withdraw funds from sender account
        //add funds to receiver account
        
        await Task.CompletedTask;
    }
}