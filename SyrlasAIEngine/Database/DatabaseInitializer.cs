using Microsoft.Data.Sqlite;
using System.IO;
using System.Threading.Tasks;

namespace SyrlasAIEngine.Database
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _dbFilePath;

        public DatabaseInitializer(string dbFilePath = "syrlas_studio.db")
        {
            _dbFilePath = dbFilePath;
            _connectionString = $"Data Source={_dbFilePath};Mode=ReadWriteCreate;";
        }

        public string GetConnectionString() => _connectionString;

        public async Task InitializeAsync()
        {
            bool dbExists = File.Exists(_dbFilePath);

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Принудительно включаем WAL и Foreign Keys для каждого соединения
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    PRAGMA foreign_keys = ON;";
                await command.ExecuteNonQueryAsync();
            }

            // Если БД только создана - накатываем схему
            if (!dbExists || new FileInfo(_dbFilePath).Length == 0)
            {
                string schemaSql = await File.ReadAllTextAsync(Path.Combine("Database", "schema.sql"));
                using var command = connection.CreateCommand();
                command.CommandText = schemaSql;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}