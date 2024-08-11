using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows;

public interface ISubscription<TMessage> where TMessage : notnull
{
    static abstract RoutingInfo Correlate(TMessage msg);
}

public interface ISubscription<TMessage1, TMessage2> 
    where TMessage1 : notnull
    where TMessage2 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
}

public interface ISubscription<TMessage1, TMessage2, TMessage3> 
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
    static abstract RoutingInfo Correlate(TMessage3 msg);
}

public interface ISubscription<TMessage1, TMessage2, TMessage3, TMessage4> 
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
    where TMessage4 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
    static abstract RoutingInfo Correlate(TMessage3 msg);
    static abstract RoutingInfo Correlate(TMessage4 msg);
}

public interface ISubscription<TMessage1, TMessage2, TMessage3, TMessage4, TMessage5> 
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
    where TMessage4 : notnull
    where TMessage5 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
    static abstract RoutingInfo Correlate(TMessage3 msg);
    static abstract RoutingInfo Correlate(TMessage4 msg);
    static abstract RoutingInfo Correlate(TMessage5 msg);
}

public interface ISubscription<TMessage1, TMessage2, TMessage3, TMessage4, TMessage5, TMessage6> 
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
    where TMessage4 : notnull
    where TMessage5 : notnull
    where TMessage6 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
    static abstract RoutingInfo Correlate(TMessage3 msg);
    static abstract RoutingInfo Correlate(TMessage4 msg);
    static abstract RoutingInfo Correlate(TMessage5 msg);
    static abstract RoutingInfo Correlate(TMessage6 msg);
}

public interface ISubscription<TMessage1, TMessage2, TMessage3, TMessage4, TMessage5, TMessage6, TMessage7> 
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
    where TMessage4 : notnull
    where TMessage5 : notnull
    where TMessage6 : notnull
    where TMessage7 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
    static abstract RoutingInfo Correlate(TMessage3 msg);
    static abstract RoutingInfo Correlate(TMessage4 msg);
    static abstract RoutingInfo Correlate(TMessage5 msg);
    static abstract RoutingInfo Correlate(TMessage6 msg);
    static abstract RoutingInfo Correlate(TMessage7 msg);
}

public interface ISubscription<TMessage1, TMessage2, TMessage3, TMessage4, TMessage5, TMessage6, TMessage7, TMessage8> 
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
    where TMessage4 : notnull
    where TMessage5 : notnull
    where TMessage6 : notnull
    where TMessage7 : notnull
    where TMessage8 : notnull
{
    static abstract RoutingInfo Correlate(TMessage1 msg);
    static abstract RoutingInfo Correlate(TMessage2 msg);
    static abstract RoutingInfo Correlate(TMessage3 msg);
    static abstract RoutingInfo Correlate(TMessage4 msg);
    static abstract RoutingInfo Correlate(TMessage5 msg);
    static abstract RoutingInfo Correlate(TMessage6 msg);
    static abstract RoutingInfo Correlate(TMessage7 msg);
    static abstract RoutingInfo Correlate(TMessage8 msg);
}