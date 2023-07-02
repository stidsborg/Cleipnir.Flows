using Cleipnir.ResilientFunctions.PostgreSQL;

namespace Cleipnir.Flows.PostgresSql;

public class PostgresFlowStore : PostgreSqlFunctionStore, IFlowStore
{
    public PostgresFlowStore(string connectionString, string tablePrefix = "") : base(connectionString, tablePrefix) { }
}