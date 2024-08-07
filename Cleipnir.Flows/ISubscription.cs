using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public interface ISubscription<TMessage> where TMessage : notnull
{
    static abstract RoutingInfo Correlate(TMessage msg);
}