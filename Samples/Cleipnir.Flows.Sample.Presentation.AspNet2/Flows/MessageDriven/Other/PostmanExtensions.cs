using Cleipnir.ResilientFunctions.Messaging;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

public static class PostmanExtensions
{
    public static Task RouteMessage(this Postman postman, object message, Type messageType)
    {
        return (Task) typeof(Postman)
            .GetMethods()
            .Single(m => m.Name == nameof(Postman.RouteMessage) && m.GetParameters().Length == 1)
            .MakeGenericMethod([messageType])
            .Invoke(postman, [message])!;
    }
}