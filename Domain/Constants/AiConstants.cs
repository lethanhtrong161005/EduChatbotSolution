namespace Domain.Constants;

public static class ChunkingStrategy
{
    public const string FixedLength = "FixedLength";
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
