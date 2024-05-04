using Serilog;

namespace Cleipnir.Flows.Sample.Presentation.A_OrderFlowRpc.Solution;

public interface ILogisticsClient
{
    Task ShipProducts(Guid customerId, IEnumerable<Guid> productIds);
}

public class LogisticsClientStub : ILogisticsClient
{
    public Task ShipProducts(Guid customerId, IEnumerable<Guid> productIds)
        => Task.Delay(100).ContinueWith(_ =>
            Log.Logger.ForContext<ILogisticsClient>().Information("LOGISTICS_SERVER: Products shipped")
        );
}