using Cleipnir.ResilientFunctions.AzureBlob;

namespace Cleipnir.Flows.AzureBlob;

public class AzureBlobFlowStore : AzureBlobFunctionStore, IFlowStore
{
    public AzureBlobFlowStore(string connectionString, string tablePrefix = "") : base(connectionString, tablePrefix) { }
}