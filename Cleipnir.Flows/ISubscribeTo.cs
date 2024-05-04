using System.Threading.Tasks;

namespace Cleipnir.Flows;

public delegate Task BusPublish(object msg);

public interface ISubscribeTo<TMessage>
{
    public BusPublish PublishOnBus(object msg);
    static abstract string Correlate(TMessage msg);
}