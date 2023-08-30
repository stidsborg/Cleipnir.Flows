using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Storage;

namespace Cleipnir.Flows.Persistence
{
    public class InMemoryFlowStore : IFlowStore
    {
        private readonly InMemoryFunctionStore _functionStore = new();
        public IEventStore EventStore => _functionStore.EventStore;

        public ITimeoutStore TimeoutStore => _functionStore.TimeoutStore;

        public Utilities Utilities => _functionStore.Utilities;
        
        public Task<bool> CreateFunction(FunctionId functionId, StoredParameter param, StoredScrapbook storedScrapbook, IEnumerable<StoredEvent>? storedEvents, long leaseExpiration, long? postponeUntil)
            => _functionStore.CreateFunction(functionId, param, storedScrapbook, storedEvents, leaseExpiration, postponeUntil);
        
        public Task<bool> DeleteFunction(FunctionId functionId, int? expectedEpoch = null)
            => _functionStore.DeleteFunction(functionId, expectedEpoch);    

        public Task<bool> FailFunction(FunctionId functionId, StoredException storedException, string scrapbookJson, int expectedEpoch, ComplimentaryState.SetResult complementaryState)
            => _functionStore.FailFunction(functionId, storedException, scrapbookJson, expectedEpoch, complementaryState);

        public Task<StatusAndEpoch?> GetFunctionStatus(FunctionId functionId)
            => _functionStore.GetFunctionStatus(functionId);

        public Task<StoredFunction?> GetFunction(FunctionId functionId)
            => _functionStore.GetFunction(functionId);

        public Task<IEnumerable<StoredExecutingFunction>> GetCrashedFunctions(FunctionTypeId functionTypeId, long leaseExpiresBefore)
            => _functionStore.GetCrashedFunctions(functionTypeId, leaseExpiresBefore);
        
        public Task<IEnumerable<StoredPostponedFunction>> GetPostponedFunctions(FunctionTypeId functionTypeId, long expiresBefore)
            => _functionStore.GetPostponedFunctions(functionTypeId, expiresBefore);

        public Task<bool> IncrementAlreadyPostponedFunctionEpoch(FunctionId functionId, int expectedEpoch)
            => _functionStore.IncrementAlreadyPostponedFunctionEpoch(functionId, expectedEpoch);

        public Task<bool> RestartExecution(FunctionId functionId, int expectedEpoch, long leaseExpiration)
            => _functionStore.RestartExecution(functionId, expectedEpoch, leaseExpiration);

        public Task<bool> RenewLease(FunctionId functionId, int expectedEpoch, long leaseExpiration)
            => _functionStore.RenewLease(functionId, expectedEpoch, leaseExpiration);

        public Task Initialize()
            => _functionStore.Initialize();

        public Task<bool> PostponeFunction(FunctionId functionId, long postponeUntil, string scrapbookJson, int expectedEpoch, ComplimentaryState.SetResult complementaryState)
            => _functionStore.PostponeFunction(functionId, postponeUntil, scrapbookJson, expectedEpoch, complementaryState);

        public Task<bool> SaveScrapbookForExecutingFunction(FunctionId functionId, string scrapbookJson, int expectedEpoch, ComplimentaryState.SaveScrapbookForExecutingFunction complimentaryState)
            => _functionStore.SaveScrapbookForExecutingFunction(functionId, scrapbookJson, expectedEpoch, complimentaryState);

        public Task<bool> SetFunctionState(FunctionId functionId, Status status, StoredParameter storedParameter, StoredScrapbook storedScrapbook, StoredResult storedResult, StoredException? storedException, long? postponeUntil, ReplaceEvents? events, int expectedEpoch)
            => _functionStore.SetFunctionState(functionId, status, storedParameter, storedScrapbook, storedResult, storedException, postponeUntil, events, expectedEpoch);

        public Task<bool> SetParameters(FunctionId functionId, StoredParameter storedParameter, StoredScrapbook storedScrapbook, ReplaceEvents? events, bool suspended, int expectedEpoch)
            => _functionStore.SetParameters(functionId, storedParameter, storedScrapbook, events, suspended, expectedEpoch);

        public Task<bool> SucceedFunction(FunctionId functionId, StoredResult result, string scrapbookJson, int expectedEpoch, ComplimentaryState.SetResult complementaryState)
            => _functionStore.SucceedFunction(functionId, result, scrapbookJson, expectedEpoch, complementaryState);

        public Task<SuspensionResult> SuspendFunction(FunctionId functionId, int expectedEventCount, string scrapbookJson, int expectedEpoch, ComplimentaryState.SetResult complementaryState)
            => _functionStore.SuspendFunction(functionId, expectedEventCount, scrapbookJson, expectedEpoch, complementaryState);
    }
}
