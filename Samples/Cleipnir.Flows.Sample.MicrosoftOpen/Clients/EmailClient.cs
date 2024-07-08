using Serilog;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

public interface IEmailClient
{
    Task SendOrderConfirmation(Guid customerId, TrackAndTrace trackAndTrace, IEnumerable<Guid> productIds);
}

public class EmailClientStub : IEmailClient
{
    public Task SendOrderConfirmation(Guid customerId, TrackAndTrace trackAndTrace, IEnumerable<Guid> productIds)
        => Task.Delay(100).ContinueWith(_ => 
            Log.Logger.ForContext<IEmailClient>().Information("EMAIL_SERVER: Order confirmation emailed")
        );
}