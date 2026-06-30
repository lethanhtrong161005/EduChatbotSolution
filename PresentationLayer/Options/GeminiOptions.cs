namespace Presentation.Options;

public sealed class GeminiOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int DefaultEmbeddingDimensions { get; set; }
}
