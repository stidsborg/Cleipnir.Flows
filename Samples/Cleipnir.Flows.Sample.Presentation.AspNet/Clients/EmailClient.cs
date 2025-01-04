using Serilog;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

public interface IEmailClient
{
    Task SendOrderConfirmation(Guid customerId, TrackAndTrace trackAndTrace, IEnumerable<Guid> productIds);

    Task<bool> IsServiceDown();
}

public class EmailClientStub : IEmailClient
{
    public Task SendOrderConfirmation(Guid customerId, TrackAndTrace trackAndTrace, IEnumerable<Guid> productIds)
        => Task.Delay(ClientSettings.Delay).ContinueWith(_ => 
            Log.Logger.ForContext<IEmailClient>().Information("EMAIL_SERVER: Order confirmation emailed")
        );

    public Task<bool> IsServiceDown() => Task.FromResult(false);
}