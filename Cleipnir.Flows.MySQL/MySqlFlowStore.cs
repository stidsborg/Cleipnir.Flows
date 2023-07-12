using Cleipnir.ResilientFunctions.MySQL;

namespace Cleipnir.Flows.MySQL;

public class MySqlFlowStore : MySqlFunctionStore, IFlowStore
{
    public MySqlFlowStore(string connectionString, string tablePrefix = "") : base(connectionString, tablePrefix) { }
}