using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.ConsoleApp.Routing;

public class SubscribeToFlow : Flow, ISubscribeTo<OrderCreated>
{
    public override Task Run()
    {
        Console.WriteLine("OK");
        return Task.CompletedTask;
    }
    
    public static RoutingInfo Correlate(OrderCreated msg) => Route.To(msg.OrderNumber);
}

public record OrderCreated(string OrderNumber, IEnumerable<string> OrderItems);