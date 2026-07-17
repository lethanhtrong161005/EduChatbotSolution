namespace Domain.Entities;

public class ChatSessionTitleGenerationMetrics : NaturalEntity
{
    public long? PromptTokens { get; set; }

    public long? CompletionTokens { get; set; }

    public long ResponseTimeMs { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;
}
