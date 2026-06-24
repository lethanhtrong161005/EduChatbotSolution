namespace Domain.Entities;

public class ChatSessionTitleGenerationSettings : NaturalEntity
{
    public string LlmModel { get; set; } = string.Empty;

    public float Temperature { get; set; }

    public string SystemPrompt { get; set; } = string.Empty;

    public virtual ChatSession ChatSession { get; set; } = null!;
}
