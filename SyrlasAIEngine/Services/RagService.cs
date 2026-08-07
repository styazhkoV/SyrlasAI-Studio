using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using SyrlasAIEngine.Database;
using SyrlasAIEngine.Models;
using SyrlasAIEngine.Services.Parsers;

namespace SyrlasAIEngine.Services
{
    public class RagService
    {
        private readonly DatabaseInitializer _dbInit;
        private readonly IEnumerable<IDocumentParser> _parsers;

        public RagService(DatabaseInitializer dbInit, IEnumerable<IDocumentParser> parsers)
        {
            _dbInit = dbInit;
            _parsers = parsers;
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_dbInit.GetConnectionString());

        public async Task<UploadArtifactResponse> ProcessAndIndexFileAsync(Stream fileStream, string fileName, string? sessionId)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            var parser = _parsers.FirstOrDefault(p => p.SupportsExtension(ext));

            if (parser == null)
            {
                throw new NotSupportedException($"Расширение файла '{ext}' не поддерживается для RAG индексации.");
            }

            // Вычисляем SHA-256 хэш файла
            fileStream.Position = 0;
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(fileStream);
            string fileHash = Convert.ToHexString(hashBytes);

            fileStream.Position = 0;
            var chunks = (await parser.ParseAsync(fileStream, fileName)).ToList();

            using var conn = GetConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            string artifactId = Guid.NewGuid().ToString();
            string fileType = GetFileTypeByExtension(ext);

            // 1. Вставляем запись в таблицу artifacts
            await conn.ExecuteAsync(@"
                INSERT INTO artifacts (id, session_id, file_name, file_path, file_extension, file_type, file_hash, file_size_bytes, status)
                VALUES (@id, @sessionId, @fileName, @filePath, @ext, @fileType, @fileHash, @fileSize, 'INDEXED')",
                new {
                    id = artifactId,
                    sessionId,
                    fileName,
                    filePath = fileName,
                    ext,
                    fileType,
                    fileHash,
                    fileSize = fileStream.Length
                }, transaction);

            // 2. Индексируем чанки в document_chunks и document_chunks_fts
            foreach (var chunk in chunks)
            {
                string chunkId = Guid.NewGuid().ToString();

                await conn.ExecuteAsync(@"
                    INSERT INTO document_chunks (id, artifact_id, chunk_index, content, metadata_json, token_count)
                    VALUES (@id, @artifactId, @chunkIndex, @content, @metadataJson, @tokenCount)",
                    new {
                        id = chunkId,
                        artifactId,
                        chunkIndex = chunk.ChunkIndex,
                        content = chunk.Content,
                        metadataJson = chunk.MetadataJson,
                        tokenCount = chunk.TokenCount
                    }, transaction);

                // Заполняем FTS5 Таблицу для полнотекстового поиска
                await conn.ExecuteAsync(@"
                    INSERT INTO document_chunks_fts (chunk_id, content, metadata_json)
                    VALUES (@chunkId, @content, @metadataJson)",
                    new { chunkId, content = chunk.Content, metadataJson = chunk.MetadataJson }, transaction);
            }

            transaction.Commit();

            return new UploadArtifactResponse
            {
                ArtifactId = artifactId,
                FileName = fileName,
                TotalChunks = chunks.Count,
                Status = "INDEXED"
            };
        }

        private static string GetFileTypeByExtension(string ext) => ext switch
        {
            ".cs" or ".java" or ".ts" or ".js" or ".py" or ".sql" => "CODE",
            ".docx" => "OFFICE_DOC",
            ".xlsx" => "EXCEL",
            ".pdf" => "PDF",
            _ => "SPECIFICATION"
        };
    }
}