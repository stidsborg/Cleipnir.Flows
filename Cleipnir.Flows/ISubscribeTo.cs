using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public interface ISubscribeTo<TMessage> where TMessage : notnull
{
    static abstract RoutingInfo Correlate(TMessage msg);
}