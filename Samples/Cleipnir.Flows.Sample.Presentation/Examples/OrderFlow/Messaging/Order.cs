namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Messaging;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);