namespace Business.Embedding;

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;
}
