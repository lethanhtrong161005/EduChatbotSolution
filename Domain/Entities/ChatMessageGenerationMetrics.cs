namespace Domain.Entities;

public class ChatMessageGenerationMetrics : NaturalEntity
{
    public int RetrievedChunkCount { get; set; }

    public int ContextChunkCount { get; set; }

    /// <summary>Gets or sets the number of prompt tokens consumed (nullable).</summary>
    public int PromptTokens { get; set; }

    /// <summary>Gets or sets the number of completion tokens generated (nullable).</summary>
    public int CompletionTokens { get; set; }

    public long RetrievalTimeMs { get; set; }

    public long TimeToFirstTokenMs { get; set; }

    public long TotalResponseTimeMs { get; set; }

    public double TokensPerSecond { get; set; }

    public virtual ChatMessage ChatMessage { get; set; } = null!;
}
