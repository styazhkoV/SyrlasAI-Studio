using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SyrlasAIEngine.Models;
using UglyToad.PdfPig;

namespace SyrlasAIEngine.Services.Parsers
{
    public class PdfPigParser : IDocumentParser
    {
        public bool SupportsExtension(string extension) => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        public Task<IEnumerable<DocumentChunkDto>> ParseAsync(Stream stream, string fileName)
        {
            var chunks = new List<DocumentChunkDto>();
            using var pdf = PdfDocument.Open(stream);

            int index = 0;
            foreach (var page in pdf.GetPages())
            {
                string pageText = page.Text;
                if (string.IsNullOrWhiteSpace(pageText)) continue;

                var meta = new { fileName, pageNumber = page.Number, type = "PDF_PAGE" };
                chunks.Add(new DocumentChunkDto
                {
                    ChunkIndex = index++,
                    Content = pageText,
                    MetadataJson = JsonSerializer.Serialize(meta),
                    TokenCount = pageText.Length / 4
                });
            }

            return Task.FromResult<IEnumerable<DocumentChunkDto>>(chunks);
        }
    }
}