using Serilog;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

public interface ILogisticsClient
{
    Task<TrackAndTrace> ShipProducts(Guid customerId, IEnumerable<Guid> productIds);
}

public record TrackAndTrace(string Value);

public class LogisticsClientStub : ILogisticsClient
{
    public Task<TrackAndTrace> ShipProducts(Guid customerId, IEnumerable<Guid> productIds)
        => Task.Delay(100).ContinueWith(_ =>
            {
                Log.Logger.ForContext<ILogisticsClient>().Information("LOGISTICS_SERVER: Products shipped");
                return new TrackAndTrace(Guid.NewGuid().ToString());
            }
        );
}