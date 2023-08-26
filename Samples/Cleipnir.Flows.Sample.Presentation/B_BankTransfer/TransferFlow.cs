namespace Cleipnir.Flows.Sample.Presentation.B_BankTransfer;

public sealed class TransferFlow : Flow<Transfer>
{
    public TransferFlow(IBankCentralClient bankCentralClient) => BankCentralClient = bankCentralClient;

    private IBankCentralClient BankCentralClient { get; }

    public override async Task Run(Transfer transfer)
    {
        //check available funds
        
        //withdraw funds from sender account
        //add funds to receiver account
    }
}