namespace SyrlasAIEngine.Models;

public class DocumentEmbedding
{
    public int Id { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // Векторное представление для работы с pgvector в виде массива чисел.
    // При отсутствии пакета Pgvector используем стандартный массив float[]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}