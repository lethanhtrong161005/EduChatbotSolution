namespace Domain.Entities;

public class ChatMessageGenerationSettings : NaturalEntity
{
    public int TopK { get; set; }

    public double SimilarityThreshold { get; set; }

    public string LlmModel { get; set; } = string.Empty;

    public double Temperature { get; set; }

    public string SystemPrompt { get; set; } = string.Empty;

    public int MaxContextChunks { get; set; }

    public int MaxHistoryMessages { get; set; }

    public virtual ChatMessage ChatMessage { get; set; } = null!;
}
