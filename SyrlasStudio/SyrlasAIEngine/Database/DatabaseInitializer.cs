using Microsoft.Data.Sqlite;

namespace SyrlasAIEngine.Database
{
    public class DatabaseInitializer
    {
        public string ConnectionString { get; }

        public DatabaseInitializer(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task InitializeAsync()
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. Создание таблицы чанков документов, если она еще не создана
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS document_chunks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    chunk_index INTEGER NOT NULL,
                    content TEXT NOT NULL,
                    metadata_json TEXT,
                    token_count INTEGER NOT NULL,
                    embedding BLOB
                );";
            await createTableCmd.ExecuteNonQueryAsync();

            // 2. Безопасная миграция: проверка и добавление колонки embedding, если база старой версии
            await EnsureColumnExistsAsync(connection, "document_chunks", "embedding", "BLOB");
        }

        private async Task EnsureColumnExistsAsync(SqliteConnection connection, string tableName, string columnName, string columnType)
        {
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = $"PRAGMA table_info({tableName});";

            bool columnExists = false;
            using var reader = await checkCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Колонка 'name' в PRAGMA table_info находится на индексе 1
                if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }

            // Если колонка отсутствует, добавляем её через ALTER TABLE
            if (!columnExists)
            {
                var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};";
                await alterCmd.ExecuteNonQueryAsync();
            }
        }
    }
}