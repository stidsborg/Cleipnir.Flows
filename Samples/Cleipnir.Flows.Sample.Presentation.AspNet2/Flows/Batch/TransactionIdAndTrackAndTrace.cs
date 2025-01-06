using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Batch;

public record TransactionIdAndTrackAndTrace(string OrderId, Guid TransactionId, TrackAndTrace TrackAndTraceNumber);