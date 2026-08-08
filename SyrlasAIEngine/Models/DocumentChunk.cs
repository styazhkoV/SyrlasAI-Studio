namespace SyrlasAIEngine.Models
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
