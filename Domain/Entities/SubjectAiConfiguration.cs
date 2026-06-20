namespace Domain.Entities;

public class SubjectAiConfiguration : CategoryLikeEntity
{
    public string? ChunkingStrategy { get; set; }

    public string? EmbeddingModel { get; set; }

    public int? TopK { get; set; }

    public double? SimilarityThreshold { get; set; }

    public string? LlmModel { get; set; }

    public double? Temperature { get; set; }

    public string? SystemPrompt { get; set; }

    public int? MaxContextChunks { get; set; }

    public int? MaxHistoryMessages { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
