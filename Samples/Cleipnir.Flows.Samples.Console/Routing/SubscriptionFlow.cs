using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.ConsoleApp.Routing;

public class SubscriptionFlow : Flow, ISubscription<OrderCreated>
{
    public override Task Run()
    {
        Console.WriteLine("OK");
        return Task.CompletedTask;
    }
    
    public static RoutingInfo Correlate(OrderCreated msg) => Route.To(msg.OrderNumber);
}

public record OrderCreated(string OrderNumber, IEnumerable<string> OrderItems);