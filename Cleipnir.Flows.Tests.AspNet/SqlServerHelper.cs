using Cleipnir.ResilientFunctions.SqlServer;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Data.SqlClient;

namespace Cleipnir.Flows.Tests.AspNet
{
    [TestClass]
    public static class SqlServerHelper
    {
        private static volatile bool _isInitialized = false;
        private static readonly Lock Lock = new();
        
        public static string ConnectionString { get; }

        static SqlServerHelper()
        {
            ConnectionString = 
                Environment.GetEnvironmentVariable("Cleipnir.RFunctions.SqlServer.Tests.ConnectionString")
                ?? "Server=localhost;Database=rfunctions;User Id=sa;Password=Pa55word!;Encrypt=True;TrustServerCertificate=True;";
        }
        
        public static void CreateDatabase()
        {
            lock (Lock)
            {
                if (_isInitialized) return;
                _isInitialized = true;
            
                var connectionStringWithoutDatabase = ResilientFunctions.Storage.DatabaseHelper.GetConnectionStringWithoutDatabase(ConnectionString);
                var databaseName = ResilientFunctions.Storage.DatabaseHelper.GetDatabaseName(ConnectionString);

                using var conn = new SqlConnection(connectionStringWithoutDatabase);
                conn.Open();
            
                Execute($"DROP DATABASE IF EXISTS {databaseName}", conn);
                Execute($"CREATE DATABASE {databaseName}", conn);                
            }
        }

        public static async Task<IFunctionStore> CreateAndInitializeStore()
        {
            CreateDatabase();
            
            var store = new SqlServerFunctionStore(ConnectionString, tablePrefix: "SqlServerFlows" + Random.Shared.Next(10_000));
            await store.Initialize();
            await store.TruncateTables();
            return store;
        }
        
        private static void Execute(string sql, SqlConnection connection)
        {
            var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}