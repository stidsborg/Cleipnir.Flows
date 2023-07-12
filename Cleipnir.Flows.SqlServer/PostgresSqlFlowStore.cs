using Cleipnir.ResilientFunctions.SqlServer;

namespace Cleipnir.Flows.SqlServer;

public class SqlServerFlowStore : SqlServerFunctionStore, IFlowStore
{
    public SqlServerFlowStore(string connectionString, string tablePrefix = "") : base(connectionString, tablePrefix) { }
}