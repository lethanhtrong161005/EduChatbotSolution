namespace Domain.Entities;

public class SubjectAiConfiguration : CategoryLikeEntity
{
    public string? ChunkingStrategy { get; set; }

    public string? EmbeddingModel { get; set; }

    public int? TopK { get; set; }

    public double? SimilarityThreshold { get; set; }

    public string? LlmModel { get; set; }

    public float? ChatTemperature { get; set; }

    public string? ChatPrompt { get; set; }

    public string? ContextPrompt { get; set; }

    public string? NoContextRetrievedPrompt { get; set; }

    public float? TitleTemperature { get; set; }

    public string? TitlePrompt { get; set; }

    public float? CitationExtractionTemperature { get; set; }

    public string? CitationExtractionPrompt { get; set; }

    public int? MaxContextChunks { get; set; }

    public int? MaxHistoryMessages { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
