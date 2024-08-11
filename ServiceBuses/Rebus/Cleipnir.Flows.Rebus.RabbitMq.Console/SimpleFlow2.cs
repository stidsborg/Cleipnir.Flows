using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Rebus.RabbitMq.Console;

public class SimpleFlow2 : Flow, ISubscription<MyMessage>
{
    public static RoutingInfo Correlate(MyMessage msg) => Route.To(msg.Value);
    
    public override async Task Run()
    {
        var msg = await Messages.FirstOfType<MyMessage>();
        System.Console.WriteLine($"SimpleFlow2({msg}) executed");
    }
}