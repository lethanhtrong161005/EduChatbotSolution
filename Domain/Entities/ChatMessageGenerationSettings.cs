namespace Domain.Entities;

public class ChatMessageGenerationSettings : NaturalEntity
{
    public string EmbeddingModel { get; set; } = string.Empty;

    public int TopK { get; set; }

    public double SimilarityThreshold { get; set; }

    public string LlmModel { get; set; } = string.Empty;

    public float Temperature { get; set; }

    public string SystemPrompt { get; set; } = string.Empty;

    public string ContextPrompt { get; set; } = string.Empty;

    public string NoContextRetrievedPrompt { get; set; } = string.Empty;

    public float CitationExtractionTemperature { get; set; }

    public string CitationExtractionPrompt { get; set; } = string.Empty;

    public int MaxContextChunks { get; set; }

    public int MaxHistoryMessages { get; set; }

    public virtual ChatMessage ChatMessage { get; set; } = null!;
}
