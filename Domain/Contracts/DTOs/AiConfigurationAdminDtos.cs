namespace Domain.Contracts.DTOs;

public record AiStringSettingDto
{
    public required string GlobalDefault { get; init; }
    public string? StoredOverride { get; init; }
    public required string EffectiveValue { get; init; }
}

public record AiIntSettingDto
{
    public required int GlobalDefault { get; init; }
    public int? StoredOverride { get; init; }
    public required int EffectiveValue { get; init; }
}

public record AiDoubleSettingDto
{
    public required double GlobalDefault { get; init; }
    public double? StoredOverride { get; init; }
    public required double EffectiveValue { get; init; }
}

public record AiFloatSettingDto
{
    public required float GlobalDefault { get; init; }
    public float? StoredOverride { get; init; }
    public required float EffectiveValue { get; init; }
}

public record AiIndexingConfigurationDto
{
    public required AiStringSettingDto ChunkingStrategy { get; init; }
    public required AiIntSettingDto ChunkSize { get; init; }
    public required AiIntSettingDto ChunkOverlap { get; init; }
    public required AiStringSettingDto EmbeddingModel { get; init; }
}

public record AiRetrievalConfigurationDto
{
    public required AiIntSettingDto TopK { get; init; }
    public required AiDoubleSettingDto SimilarityThreshold { get; init; }
    public required AiIntSettingDto MaxContextChunks { get; init; }
}

public record AiGenerationConfigurationDto
{
    public required AiStringSettingDto LlmModel { get; init; }
    public required AiFloatSettingDto ChatTemperature { get; init; }
    public required AiIntSettingDto MaxHistoryMessages { get; init; }
    public required AiFloatSettingDto TitleTemperature { get; init; }
    public required AiFloatSettingDto CitationExtractionTemperature { get; init; }
}

public record AiPromptConfigurationDto
{
    public required AiStringSettingDto ChatPrompt { get; init; }
    public required AiStringSettingDto ContextPrompt { get; init; }
    public required AiStringSettingDto NoContextRetrievedPrompt { get; init; }
    public required AiStringSettingDto TitlePrompt { get; init; }
    public required AiStringSettingDto CitationExtractionPrompt { get; init; }
}

public record SubjectAiConfigurationDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required AiIndexingConfigurationDto Indexing { get; init; }
    public required AiRetrievalConfigurationDto Retrieval { get; init; }
    public required AiGenerationConfigurationDto Generation { get; init; }
    public required AiPromptConfigurationDto Prompts { get; init; }
}

public record SaveSubjectAiConfigurationRequest
{
    public string? ChunkingStrategy { get; init; }
    public int? ChunkSize { get; init; }
    public int? ChunkOverlap { get; init; }
    public string? EmbeddingModel { get; init; }
    public int? TopK { get; init; }
    public double? SimilarityThreshold { get; init; }
    public int? MaxContextChunks { get; init; }
    public string? LlmModel { get; init; }
    public float? ChatTemperature { get; init; }
    public int? MaxHistoryMessages { get; init; }
    public string? ChatPrompt { get; init; }
    public string? ContextPrompt { get; init; }
    public string? NoContextRetrievedPrompt { get; init; }
    public float? TitleTemperature { get; init; }
    public string? TitlePrompt { get; init; }
    public float? CitationExtractionTemperature { get; init; }
    public string? CitationExtractionPrompt { get; init; }
}

public record AiOptionDto
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

public record AiConfigurationOptionsDto
{
    public required IReadOnlyList<AiOptionDto> ChunkingStrategies { get; init; }
    public required IReadOnlyList<AiOptionDto> EmbeddingModels { get; init; }
    public required IReadOnlyList<AiOptionDto> ChatModels { get; init; }
    public required IReadOnlyList<AiOptionDto> JudgeModels { get; init; }
}

public record AiConfigurationSubjectOptionDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
}

public record SubjectReindexResponseDto
{
    public required int SubjectId { get; init; }
    public required int QueuedDocumentCount { get; init; }
    public required DateTime QueuedAt { get; init; }
}
