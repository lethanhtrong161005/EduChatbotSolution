namespace Domain.Entities;

public class SubjectAiConfiguration : CategoryLikeEntity
{
    public string ChunkingStrategy { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public int TopK { get; set; }

    public string LlmModel { get; set; } = string.Empty;

    public double Temperature { get; set; }

    public string SystemPrompt { get; set; } = string.Empty;

    public int MaxContextChunks { get; set; }

    public int MaxHistoryMessages { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
