using System.Threading.Tasks;

namespace Cleipnir.Flows.Rebus;

public class RebusGenericHandler<TFlows>(TFlows flows)
    where TFlows : IBaseFlows
{
    public Task HandleIncomingMessage<T>(T message) where T : class
        => flows.RouteMessage(message);
}