using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Services.Parsers
{
    public interface IDocumentParser
    {
        bool SupportsExtension(string extension);
        Task<IEnumerable<DocumentChunkDto>> ParseAsync(Stream stream, string fileName);
    }
}