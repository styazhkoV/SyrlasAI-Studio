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
    public interface IEmbeddingGenerator
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
    }

    public class RagService
    {
        private readonly DatabaseInitializer _dbInitializer;
        private readonly IEnumerable<IDocumentParser> _parsers;
        private readonly IEmbeddingGenerator? _embeddingGenerator;

        public RagService(
            DatabaseInitializer dbInitializer, 
            IEnumerable<IDocumentParser> parsers,
            IEmbeddingGenerator? embeddingGenerator = null)
        {
            _dbInitializer = dbInitializer;
            _parsers = parsers;
            _embeddingGenerator = embeddingGenerator;
        }

        // 1. Индексация загруженного файла с расчетом эмбеддингов
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
                // Генерация эмбеддинга для чанка (если доступен генератор)
                float[]? embedding = _embeddingGenerator != null 
                    ? await _embeddingGenerator.GenerateEmbeddingAsync(chunk.Content) 
                    : null;

                byte[]? embeddingBlob = embedding != null ? ToByteArray(embedding) : null;

                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO document_chunks (chunk_index, content, metadata_json, token_count, embedding)
                    VALUES (@chunkIndex, @content, @metadataJson, @tokenCount, @embedding);";

                command.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
                command.Parameters.AddWithValue("@content", chunk.Content);
                command.Parameters.AddWithValue("@metadataJson", chunk.MetadataJson ?? string.Empty);
                command.Parameters.AddWithValue("@tokenCount", chunk.TokenCount);
                command.Parameters.AddWithValue("@embedding", (object?)embeddingBlob ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        // 2. Семантический поиск по вектору или fallback на ключевые слова
        public async Task<string> SearchContextAsync(string query, int limit = 3, float similarityThreshold = 0.6f)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            using var connection = new SqliteConnection(_dbInitializer.ConnectionString);
            await connection.OpenAsync();

            var results = new List<string>();

            // Если есть генератор эмбеддингов, выполняем семантический поиск по косинусному расстоянию
            if (_embeddingGenerator != null)
            {
                var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query);
                
                var command = connection.CreateCommand();
                command.CommandText = "SELECT content, embedding FROM document_chunks WHERE embedding IS NOT NULL;";

                using var reader = await command.ExecuteReaderAsync();
                var scoredChunks = new List<(string Content, float Score)>();

                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(1))
                    {
                        byte[] blob = (byte[])reader["embedding"];
                        float[] chunkEmbedding = FromByteArray(blob);
                        float score = CosineSimilarity(queryEmbedding, chunkEmbedding);

                        if (score >= similarityThreshold)
                        {
                            scoredChunks.Add((reader.GetString(0), score));
                        }
                    }
                }

                results = scoredChunks
                    .OrderByDescending(x => x.Score)
                    .Take(limit)
                    .Select(x => x.Content)
                    .ToList();
            }

            // Fallback: если эмбеддинги не настроены или ничего не нашлось, ищем по ключевым словам (LIKE)
            if (results.Count == 0)
            {
                results = await SearchByKeywordsFallbackAsync(connection, query, limit);
            }

            return string.Join("\n---\n", results);
        }

        private async Task<List<string>> SearchByKeywordsFallbackAsync(SqliteConnection connection, string query, int limit)
        {
            var results = new List<string>();
            var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Where(k => k.Length > 2)
                                .Take(5)
                                .ToList();

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

            return results;
        }

        // Вспомогательные математические методы для векторов
        private static float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 0f;

            float dotProduct = 0f, normA = 0f, normB = 0f;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0f || normB == 0f) return 0f;
            return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
        }

        private static byte[] ToByteArray(float[] floatArray)
        {
            byte[] byteArray = new byte[floatArray.Length * 4];
            Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        private static float[] FromByteArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 4];
            Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);
            return floatArray;
        }
    }
}