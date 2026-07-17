namespace Domain.Constants;

public static class ChunkingStrategy
{
    public const string FixedLength = "FixedLength";
    public const string RecursiveSeparator = "RecursiveSeparator";
    public const string SentenceParagraph = "SentenceParagraph";
}

public static class EmbeddingModelName
{
    public const string BgeM3 = "bge-m3";
    public const string NemotronEmbedVLFree = "nvidia/llama-nemotron-embed-vl-1b-v2:free";
    public const string GeminiEmbedding2 = "gemini-embedding-2";
}

public static class ChatModelName
{
    public const string Qwen3 = "qwen3";
    public const string Qwen35 = "qwen3.5";
    public const string OpenRouterFree = "openrouter/free";
    public const string Gemini31ProPreview = "gemini-3.1-pro-preview";
    public const string Gemini35Flash = "gemini-3.5-flash";
}

public static class AiProviderName
{
    public const string Ollama = "ollama";
    public const string OpenRouter = "openrouter";
    public const string Gemini = "gemini";

    public static string ForEmbeddingModel(string model) => model switch
    {
        EmbeddingModelName.BgeM3 => Ollama,
        EmbeddingModelName.NemotronEmbedVLFree => OpenRouter,
        EmbeddingModelName.GeminiEmbedding2 => Gemini,
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown embedding model provider."),
    };

    public static string ForChatModel(string model) => model switch
    {
        ChatModelName.Qwen3 or ChatModelName.Qwen35 => Ollama,
        ChatModelName.OpenRouterFree => OpenRouter,
        ChatModelName.Gemini31ProPreview or ChatModelName.Gemini35Flash => Gemini,
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown chat model provider."),
    };
}
