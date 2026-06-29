namespace Presentation.Options;

public sealed class OpenRouterOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int DefaultEmbeddingDimensions { get; set; }
}
