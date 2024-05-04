namespace Cleipnir.Flows.Sample.Presentation.A_OrderFlowRpc.Solution;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);