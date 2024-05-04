namespace Cleipnir.Flows.Sample.Presentation.B_OrderFlow_Messaging.Solution;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);