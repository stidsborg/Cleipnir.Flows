namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Batch;

public record OrdersBatch(string BatchId, List<Order> Orders);