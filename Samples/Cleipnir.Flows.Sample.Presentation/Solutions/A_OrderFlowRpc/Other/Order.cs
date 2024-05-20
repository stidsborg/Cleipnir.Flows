namespace Cleipnir.Flows.Sample.Presentation.Solutions.A_OrderFlowRpc;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);