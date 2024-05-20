namespace Cleipnir.Flows.Sample.Presentation.Solutions.B_OrderFlowMessaging.Other;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);