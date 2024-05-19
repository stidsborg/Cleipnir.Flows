using Cleipnir.ResilientFunctions.MySQL;
using MySqlConnector;

namespace Cleipnir.Flows.Tests.AspNet
{
    public static class MySqlHelper
    {
        public static string ConnectionString { get; }
        public static Func<Task<MySqlConnection>> ConnFunc { get; set; }
        
        static MySqlHelper()
        {
            ConnectionString = 
                Environment.GetEnvironmentVariable("Cleipnir.RFunctions.MySQL.Tests.ConnectionString")
                ?? "server=localhost;userid=root;password=Pa55word!;AllowPublicKeyRetrieval=True;;database=rfunctions_tests;";
            ConnFunc = async () =>
            {
                var conn = new MySqlConnection(ConnectionString);
                await conn.OpenAsync();
                return conn;
            };
        }
        
        public static void CreateDatabase()
        {
            // DROP test database if exists and create it again
            var database = ResilientFunctions.Storage.DatabaseHelper.GetDatabaseName(ConnectionString);

            var connectionStringWithoutDatabase = ResilientFunctions.Storage.DatabaseHelper.GetConnectionStringWithoutDatabase(ConnectionString);

            using var conn = new MySqlConnection(connectionStringWithoutDatabase);
            conn.Open();
            {
                using var command = new MySqlCommand($"DROP DATABASE IF EXISTS {database}", conn);
                command.ExecuteNonQuery();    
            }
            {
                using var command = new MySqlCommand($"CREATE DATABASE {database}", conn);
                command.ExecuteNonQuery();    
            }
        }

        public static async Task<MySqlFunctionStore> CreateAndInitializeMySqlStore()
        {
            CreateDatabase();
            
            var store = new MySqlFunctionStore(ConnectionString, tablePrefix: "MySqlFlows");
            await store.DropIfExists();
            await store.Initialize();
            return store;
        }
    }
}