namespace Cleipnir.Flows.Sample.Presentation.J_OrderSupervisor;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);