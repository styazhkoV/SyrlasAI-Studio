using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Services.Parsers
{
    public class SourceCodeParser : IDocumentParser
    {
        private static readonly HashSet<string> SupportedExts = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".c", ".cpp", ".cc", ".cxx", ".h", ".hpp"
        };

        public bool SupportsExtension(string extension) => SupportedExts.Contains(extension);

        public async Task<IEnumerable<DocumentChunkDto>> ParseAsync(Stream stream, string fileName)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string code = await reader.ReadToEndAsync();

            var chunks = new List<DocumentChunkDto>();
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            // Логическое разделение на функции/блоки или чанки по границам методов
            var blocks = SplitCodeIntoBlocks(code, ext);
            int index = 0;

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;

                chunks.Add(new DocumentChunkDto
                {
                    ChunkIndex = index++,
                    Content = block,
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(new 
                    { 
                        fileName, 
                        language = GetLanguageName(ext),
                        type = "CODE_BLOCK" 
                    }),
                    TokenCount = block.Length / 4 // Приблизительный расчёт токенов
                });
            }

            return chunks;
        }

        private static List<string> SplitCodeIntoBlocks(string code, string ext)
        {
            var result = new List<string>();
            var lines = code.Split('\n');
            var currentChunk = new StringBuilder();
            int currentTokenEst = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');
                currentChunk.AppendLine(line);
                currentTokenEst += line.Length / 4;

                // Граница логического блока: сигнатура функции/класса или достижение предела размера (~500 токенов)
                bool isBlockEnd = line.StartsWith("}") || currentTokenEst >= 500;

                if (isBlockEnd && currentChunk.Length > 0)
                {
                    result.Add(currentChunk.ToString());
                    currentChunk.Clear();
                    currentTokenEst = 0;
                }
            }

            if (currentChunk.Length > 0)
            {
                result.Add(currentChunk.ToString());
            }

            return result;
        }

        private static string GetLanguageName(string ext) => ext switch
        {
            ".cs" => "csharp",
            ".c" => "c",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".h" or ".hpp" => "cpp-header",
            _ => "text"
        };
    }
}