namespace Domain.Constants;

public static class ChunkingStrategy
{
    public const string FixedLength = "FixedLength";
}

public static class EmbeddingModelName
{
    public const string BgeM3 = "bge-m3";
    public const string NemotronEmbedVL_Free = "nvidia/llama-nemotron-embed-vl-1b-v2:free";
}

public static class ChatModelName
{
    public const string Qwen3 = "qwen3";
    public const string Qwen35 = "qwen3.5";
    public const string OpenRouterFree = "openrouter/free";
}
