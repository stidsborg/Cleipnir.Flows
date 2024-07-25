using System;
using System.Collections.Frozen;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.Domain;
using MassTransit;

namespace Cleipnir.Flows.MassTransit;

public class MassTransitGenericHandler<TFlows, TFlow>(TFlows flows)
    where TFlows : IBaseFlows
{

    private static readonly FrozenDictionary<Type, Func<object, RoutingInfo>> _consumeContextRoutes;

    static MassTransitGenericHandler() =>
        _consumeContextRoutes = typeof(TFlow)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == nameof(ISubscription<string>.Correlate))
            .Where(m => m.GetParameters().Length == 1)
            .Where(m =>
                m.GetParameters()[0].ParameterType.IsGenericType &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ConsumeContext<>)
            )
            .Select(m => new
            {
                MessageType = m.GetParameters()[0].ParameterType.GetGenericArguments()[0],
                RouteResolver = new Func<object, RoutingInfo>(msg => (RoutingInfo)m.Invoke(null, [msg])!)
            })
            .ToFrozenDictionary(
                a => a.MessageType,
                a => a.RouteResolver
            );
        
    public async Task HandleIncomingMessage<T>(ConsumeContext<T> messageContext) where T : class
    {
        var postman = flows.Postman;
        if (!_consumeContextRoutes.ContainsKey(typeof(T)))
        {
            await postman.RouteMessage(messageContext.Message);
            return;
        }

        var routeResolver = _consumeContextRoutes[typeof(T)];
        var routingInfo = routeResolver(messageContext);
        await postman.RouteMessage(messageContext.Message, routingInfo);
    }
}