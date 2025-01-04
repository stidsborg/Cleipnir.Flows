using Serilog;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

public interface IPaymentProviderClient
{
    Task Reserve(Guid transactionId, Guid customerId, decimal amount);
    Task Capture(Guid transactionId);
    Task CancelReservation(Guid transactionId);
    bool IsServiceDown();
}

public class PaymentProviderClientStub : IPaymentProviderClient
{
    public Task Reserve(Guid transactionId, Guid customerId, decimal amount)
        => Task.Delay(ClientSettings.Delay).ContinueWith(_ =>
            Log.Logger.ForContext<IPaymentProviderClient>().Information($"PAYMENT_PROVIDER: Reserved '{amount}'")
        );
    
    public Task Capture(Guid transactionId) 
        => Task.Delay(ClientSettings.Delay).ContinueWith(_ => 
            Log.Logger.ForContext<IPaymentProviderClient>().Information("PAYMENT_PROVIDER: Reserved amount captured")
        );
    public Task CancelReservation(Guid transactionId) 
        => Task.Delay(ClientSettings.Delay).ContinueWith(_ => 
            Log.Logger.ForContext<IPaymentProviderClient>().Information("PAYMENT_PROVIDER: Reservation cancelled")
        );

    public bool IsServiceDown() => false;
}