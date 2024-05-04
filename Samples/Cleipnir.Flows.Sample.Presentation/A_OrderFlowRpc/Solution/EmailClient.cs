using Serilog;

namespace Cleipnir.Flows.Sample.Presentation.A_OrderFlowRpc.Solution;

public interface IEmailClient
{
    Task SendOrderConfirmation(Guid customerId, IEnumerable<Guid> productIds);
}

public class EmailClientStub : IEmailClient
{
    public Task SendOrderConfirmation(Guid customerId, IEnumerable<Guid> productIds)
        => Task.Delay(100).ContinueWith(_ => 
            Log.Logger.ForContext<IEmailClient>().Information("EMAIL_SERVER: Order confirmation emailed")
        );
}