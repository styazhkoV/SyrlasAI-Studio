using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Services.Parsers
{
    public class CodeFileParser : IDocumentParser
    {
        private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".java", ".ts", ".js", ".py", ".sql", ".json", ".xml", ".html", ".css"
        };

        public bool SupportsExtension(string extension) => Extensions.Contains(extension);

        public async Task<IEnumerable<DocumentChunkDto>> ParseAsync(Stream stream, string fileName)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string code = await reader.ReadToEndAsync();

            var chunks = new List<DocumentChunkDto>();
            // Разбиваем код на смысловые блоки по двум переводам строк или объявлениям классов/методов
            string[] blocks = Regex.Split(code, @"(?<=\n)\s*(?=(class|public|private|protected|internal|async|function|def|interface)\s+)");

            int index = 0;
            var currentChunk = new StringBuilder();

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;

                if (currentChunk.Length + block.Length > 1500 && currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(index++, currentChunk.ToString(), fileName));
                    currentChunk.Clear();
                }

                currentChunk.AppendLine(block);
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(index++, currentChunk.ToString(), fileName));
            }

            return chunks;
        }

        private static DocumentChunkDto CreateChunk(int index, string content, string fileName)
        {
            var meta = new { fileName, type = "CODE_BLOCK" };
            return new DocumentChunkDto
            {
                ChunkIndex = index,
                Content = content,
                MetadataJson = JsonSerializer.Serialize(meta),
                TokenCount = content.Length / 4 // Примерный подсчет токенов
            };
        }
    }
}