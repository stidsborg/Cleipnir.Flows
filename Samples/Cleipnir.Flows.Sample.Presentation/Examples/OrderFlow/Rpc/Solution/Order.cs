namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Rpc.Solution;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);