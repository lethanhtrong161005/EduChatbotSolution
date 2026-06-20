using Domain.Common;

namespace Domain.Entities;

public class GlobalAiConfiguration : CategoryLikeEntity
{
    public string ChunkingStrategy { get; set; } = "FixedLength";

    public string EmbeddingModel { get; set; } = EmbeddingModelNames.BgeM3;

    public int TopK { get; set; } = 15;

    public double SimilarityThreshold { get; set; } = 0.65;

    public string LlmModel { get; set; } = ChatModelNames.Qwen3;

    public double Temperature { get; set; } = 0.3;

    public string SystemPrompt { get; set; } =
        """
        You are EduChatAI, an educational assistant.

        Answer using the provided context whenever possible.

        If the context does not contain sufficient information,
        explicitly state that the answer is not present in the course materials.

        Do not fabricate citations.

        Prefer concise, accurate answers over lengthy speculation.
        """;

    public int MaxContextChunks { get; set; } = 8;

    public int MaxHistoryMessages { get; set; } = 12;
}
