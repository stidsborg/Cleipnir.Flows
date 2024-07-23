using System.Threading.Tasks;
using MassTransit;

namespace Cleipnir.Flows.MassTransit;

public class MassTransitGenericHandler<TFlows>(TFlows flows)
    where TFlows : IBaseFlows
{
    public Task HandleIncomingMessage<T>(ConsumeContext<T> messageContext) where T : class
        => flows.RouteMessage(messageContext.Message);
}