namespace Domain.Entities;

public class SubjectAiConfiguration : CategoryLikeEntity
{
    public int SubjectId { get; set; }

    public string ChunkingStrategy { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public int RetrievalTopK { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
