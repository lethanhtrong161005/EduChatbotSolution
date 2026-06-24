namespace Domain.Entities;

public class ChatSessionTitleGenerationMetrics : NaturalEntity
{
    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public long ResponseTimeMs { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;
}
