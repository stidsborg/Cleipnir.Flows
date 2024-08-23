namespace Cleipnir.Flows.NServiceBus.Console;

public record MyMessage(string Value) : IEvent;