using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Models;
using SyrlasAIEngine.Services.Parsers;

namespace SyrlasAIEngine.Services
{
    public class RagService
    {
        private readonly DatabaseInitializer _dbInitializer;
        private readonly IEnumerable<IDocumentParser> _parsers;

        public RagService(DatabaseInitializer dbInitializer, IEnumerable<IDocumentParser> parsers)
        {
            _dbInitializer = dbInitializer;
            _parsers = parsers;
        }

        // Индексация загруженного файла
        public async Task ProcessAndIndexFileAsync(Stream stream, string fileName, string? fileCategory = null)
        {
            string ext = Path.GetExtension(fileName);
            var parser = _parsers.FirstOrDefault(p => p.SupportsExtension(ext));

            if (parser == null)
                throw new NotSupportedException($"Формат файла {ext} не поддерживается.");

            var chunks = await parser.ParseAsync(stream, fileName);

            using var connection = new SqliteConnection(_dbInitializer.ConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            foreach (var chunk in chunks)
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO document_chunks (chunk_index, content, metadata_json, token_count)
                    VALUES (@chunkIndex, @content, @metadataJson, @tokenCount);";

                command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
                command.Parameters.AddWithValue("@content", chunk.Content);
                command.Parameters.AddWithValue("@metadataJson", chunk.MetadataJson ?? string.Empty);
                command.Parameters.AddWithValue("@tokenCount", chunk.TokenCount);

                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        // Поиск контекста по ключевым словам
        public async Task<string> SearchContextAsync(string query, int limit = 3)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            var results = new List<string>();
            var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Where(k => k.Length > 2)
                                .Take(5)
                                .ToList();

            using var connection = new SqliteConnection(_dbInitializer.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();

            if (keywords.Count > 0)
            {
                var whereClauses = new List<string>();
                for (int i = 0; i < keywords.Count; i++)
                {
                    string paramName = $"@kw{i}";
                    whereClauses.Add($"content LIKE {paramName}");
                    command.Parameters.AddWithValue(paramName, $"%{keywords[i]}%");
                }

                command.CommandText = $@"
                    SELECT content 
                    FROM document_chunks 
                    WHERE {string.Join(" OR ", whereClauses)}
                    LIMIT @limit;";
            }
            else
            {
                command.CommandText = @"
                    SELECT content 
                    FROM document_chunks 
                    LIMIT @limit;";
            }

            command.Parameters.AddWithValue("@limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return string.Join("\n---\n", results);
        }
    }
}
