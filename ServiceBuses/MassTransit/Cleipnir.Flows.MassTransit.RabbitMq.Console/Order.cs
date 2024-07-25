namespace Cleipnir.Flows.MassTransit.RabbitMq.Console;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);