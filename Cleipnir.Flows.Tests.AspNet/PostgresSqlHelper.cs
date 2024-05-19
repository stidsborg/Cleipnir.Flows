using Cleipnir.ResilientFunctions.PostgreSQL;
using Npgsql;

namespace Cleipnir.Flows.Tests.AspNet
{
    public static class PostgresSqlHelper
    {
        public static string ConnectionString { get; }

        static PostgresSqlHelper()
        {
            ConnectionString = 
                Environment.GetEnvironmentVariable("Cleipnir.RFunctions.PostgreSQL.Tests.ConnectionString")
                ?? "Server=localhost;Database=rfunctions;User Id=postgres;Password=Pa55word!; Include Error Detail=true;";
        }
        
        public static void CreateDatabase()
        {
            var connectionStringWithoutDatabase = ResilientFunctions.Storage.DatabaseHelper.GetConnectionStringWithoutDatabase(ConnectionString);
            var databaseName = ResilientFunctions.Storage.DatabaseHelper.GetDatabaseName(ConnectionString);
            
            using var conn = new NpgsqlConnection(connectionStringWithoutDatabase);
            conn.Open();
            {
                using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {databaseName}", conn);
                command.ExecuteNonQuery();    
            }
            {
                using var command = new NpgsqlCommand($"CREATE DATABASE {databaseName}", conn);
                command.ExecuteNonQuery();    
            }
        }

        public static async Task<PostgreSqlFunctionStore> CreateAndInitializeStore()
        {
            CreateDatabase();
            
            var store = new PostgreSqlFunctionStore(ConnectionString, tablePrefix: "PostgresSqlFlows"); 
            await store.Initialize();
            await store.TruncateTable();
            return store;
        }
    }
}