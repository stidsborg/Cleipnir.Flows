using Cleipnir.ResilientFunctions.PostgreSQL;
using Cleipnir.ResilientFunctions.Storage;
using Npgsql;

namespace Cleipnir.Flows.Tests.AspNet
{
    public static class PostgresSqlHelper
    {
        private static volatile bool _isInitialized = false;
        private static readonly Lock Lock = new();
        
        public static string ConnectionString { get; }

        static PostgresSqlHelper()
        {
            ConnectionString = 
                Environment.GetEnvironmentVariable("Cleipnir.RFunctions.PostgreSQL.Tests.ConnectionString")
                ?? "Server=localhost;Database=rfunctions;User Id=postgres;Password=Pa55word!; Include Error Detail=true;";
        }
        
        public static void CreateDatabase()
        {
            lock (Lock)
            {
                if (_isInitialized) return;
                _isInitialized = true;
            
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
        }

        public static async Task<IFunctionStore> CreateAndInitializeStore()
        {
            CreateDatabase();
            
            var store = new PostgreSqlFunctionStore(ConnectionString, tablePrefix: "PostgresSqlFlows" + Random.Shared.Next(10_000)); 
            await store.Initialize();
            await store.TruncateTables();
            return store;
        }
    }
}