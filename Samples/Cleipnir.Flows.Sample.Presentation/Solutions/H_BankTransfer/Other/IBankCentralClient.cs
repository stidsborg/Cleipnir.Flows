using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.H_BankTransfer.Other;

public interface IBankCentralClient
{
    Task PostTransaction(Guid transactionId, string account, decimal amount);
    Task<decimal> GetAvailableFunds(string account);
}

public class BankCentralClient : IBankCentralClient
{
    public Task PostTransaction(Guid transactionId, string account, decimal amount)
    {
        Console.WriteLine($"POSTING: {amount} to {account} account");
        return Task.Delay(1_000).ContinueWith(_ => true);
    }

    public Task<decimal> GetAvailableFunds(string account) => 100M.ToTask();
}