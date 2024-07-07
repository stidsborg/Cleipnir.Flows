namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows;

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);