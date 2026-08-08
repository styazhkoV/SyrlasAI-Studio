namespace SyrlasAIEngine.Models
{
    public class DocumentChunkDto
    {
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MetadataJson { get; set; } = "{}";
        public int TokenCount { get; set; }
    }

    public class UploadArtifactResponse
    {
        public string ArtifactId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public string Status { get; set; } = "INDEXED";
    }
}
