namespace Cleipnir.Flows.Sample.Presentation.OutBoxPattern;

public interface IBus
{
    public Task Publish(object message);
}