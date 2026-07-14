using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;

namespace Business.Services.AI;

public sealed class AiConfigurationAdminService(
    IAiConfigurationResolver resolver,
    IUnitOfWork unitOfWork,
    ISubjectReindexDispatcher reindexDispatcher)
    : IAiConfigurationAdminService
{
    private readonly IAiConfigurationResolver _resolver = resolver;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ISubjectReindexDispatcher _reindexDispatcher = reindexDispatcher;

    private static readonly string[] Strategies = [ChunkingStrategy.FixedLength, ChunkingStrategy.RecursiveSeparator, ChunkingStrategy.SentenceParagraph];
    private static readonly string[] EmbeddingModels = [EmbeddingModelName.BgeM3, EmbeddingModelName.NemotronEmbedVLFree, EmbeddingModelName.GeminiEmbedding2];
    private static readonly string[] ChatModels = [ChatModelName.Qwen3, ChatModelName.OpenRouterFree, ChatModelName.Gemini31ProPreview, ChatModelName.Gemini35Flash];
    private static readonly string[] JudgeModels = [ChatModelName.Qwen3, ChatModelName.Gemini31ProPreview, ChatModelName.Gemini35Flash];

    public async Task<IReadOnlyList<AiConfigurationSubjectOptionDto>> GetSubjectsAsync(CancellationToken cxlTkn = default) =>
        [.. await _unitOfWork.Subjects.GetAsync(
            projection: e => new AiConfigurationSubjectOptionDto { SubjectId = e.Id, SubjectCode = e.Code, SubjectName = e.Name },
            orderBy: q => q.OrderBy(e => e.SubjectCode),
            asNoTracking: true,
            cancellationToken: cxlTkn)];

    public Task<AiConfigurationOptionsDto> GetOptionsAsync(CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        return Task.FromResult(new AiConfigurationOptionsDto
        {
            ChunkingStrategies =
            [
                Option(ChunkingStrategy.FixedLength, "Fixed length"),
                Option(ChunkingStrategy.RecursiveSeparator, "Recursive separators"),
                Option(ChunkingStrategy.SentenceParagraph, "Sentence / paragraph"),
            ],
            EmbeddingModels =
            [
                Option(EmbeddingModelName.BgeM3, "BGE-M3 · Ollama"),
                Option(EmbeddingModelName.NemotronEmbedVLFree, "Llama Nemotron Embed VL · OpenRouter"),
                Option(EmbeddingModelName.GeminiEmbedding2, "Gemini Embedding 2"),
            ],
            ChatModels =
            [
                Option(ChatModelName.Qwen3, "Qwen 3 · Ollama"),
                Option(ChatModelName.OpenRouterFree, "OpenRouter Free"),
                Option(ChatModelName.Gemini31ProPreview, "Gemini 3.1 Pro Preview"),
                Option(ChatModelName.Gemini35Flash, "Gemini 3.5 Flash"),
            ],
            JudgeModels =
            [
                Option(ChatModelName.Qwen3, "Qwen 3 · Ollama"),
                Option(ChatModelName.Gemini31ProPreview, "Gemini 3.1 Pro Preview"),
                Option(ChatModelName.Gemini35Flash, "Gemini 3.5 Flash"),
            ],
        });
    }

    public async Task<SubjectAiConfigurationDto?> GetSubjectConfigurationAsync(int subjectId, CancellationToken cxlTkn = default)
    {
        var subject = await _unitOfWork.Subjects.FindByIdAsync(subjectId, cxlTkn);
        if (subject == null) return null;

        var global = await GetGlobalConfigAsync(cxlTkn);
        var stored = await _unitOfWork.SubjectAiConfigurations.FindByIdAsync(subjectId, cxlTkn);
        return Map(subject, global, stored);
    }

    public async Task<SubjectAiConfigurationDto> SaveSubjectConfigurationAsync(int subjectId, SaveSubjectAiConfigurationRequest request, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = await _unitOfWork.Subjects.FindByIdAsync(subjectId, cxlTkn) ?? throw new EntityNotFoundException(subjectId);
        var global = await GetGlobalConfigAsync(cxlTkn);

        Validate(request, global);

        var desired = new SubjectAiConfiguration { Id = subjectId };
        Apply(desired, request);

        var stored = await _unitOfWork.SubjectIndexes.SaveConfigurationAsync(desired, cxlTkn);
        return Map(subject, global, stored);
    }

    public async Task<SubjectReindexResponseDto> ReindexSubjectAsync(int subjectId, CancellationToken cxlTkn = default)
    {
        var previous = await _unitOfWork.SubjectIndexes.AcquireExclusiveReindexAsync(subjectId, cxlTkn);

        try
        {
            var config = await _resolver.GetAiConfigurationAsync(subjectId, cxlTkn);
            ValidateIndexing(config.ChunkingStrategy, config.ChunkSize, config.ChunkOverlap, config.EmbeddingModel);

            var count = await _unitOfWork.ExperimentRuns.CountAffectedDocumentsAsync(subjectId, config.ChunkingStrategy, config.ChunkSize, config.ChunkOverlap, config.EmbeddingModel, cxlTkn);

            _reindexDispatcher.Enqueue(subjectId, config);
            return new SubjectReindexResponseDto { SubjectId = subjectId, QueuedDocumentCount = count, QueuedAt = DateTime.UtcNow };
        }
        catch
        {
            await _unitOfWork.SubjectIndexes.SetAvailabilityAsync(subjectId, previous, CancellationToken.None);
            throw;
        }
    }

    private async Task<GlobalAiConfiguration> GetGlobalConfigAsync(CancellationToken cxlTkn) =>
        (await _unitOfWork.GlobalAiConfigurations.GetAsync(orderBy: q => q.OrderBy(e => e.Id), asNoTracking: true, cancellationToken: cxlTkn)).FirstOrDefault()
        ?? throw new InvalidOperationException("Global AI configuration has not been initialized.");

    private static SubjectAiConfigurationDto Map(Subject subject, GlobalAiConfiguration global, SubjectAiConfiguration? stored) => new()
    {
        SubjectId = subject.Id,
        SubjectCode = subject.Code,
        SubjectName = subject.Name,
        Indexing = new AiIndexingConfigurationDto
        {
            ChunkingStrategy = Setting(global.ChunkingStrategy, stored?.ChunkingStrategy),
            ChunkSize = Setting(global.ChunkSize, stored?.ChunkSize),
            ChunkOverlap = Setting(global.ChunkOverlap, stored?.ChunkOverlap),
            EmbeddingModel = Setting(global.EmbeddingModel, stored?.EmbeddingModel),
        },
        Retrieval = new AiRetrievalConfigurationDto
        {
            TopK = Setting(global.TopK, stored?.TopK),
            SimilarityThreshold = Setting(global.SimilarityThreshold, stored?.SimilarityThreshold),
            MaxContextChunks = Setting(global.MaxContextChunks, stored?.MaxContextChunks),
        },
        Generation = new AiGenerationConfigurationDto
        {
            LlmModel = Setting(global.LlmModel, stored?.LlmModel),
            ChatTemperature = Setting(global.ChatTemperature, stored?.ChatTemperature),
            MaxHistoryMessages = Setting(global.MaxHistoryMessages, stored?.MaxHistoryMessages),
            TitleTemperature = Setting(global.TitleTemperature, stored?.TitleTemperature),
            CitationExtractionTemperature = Setting(global.CitationExtractionTemperature, stored?.CitationExtractionTemperature),
        },
        Prompts = new AiPromptConfigurationDto
        {
            ChatPrompt = Setting(global.ChatPrompt, stored?.ChatPrompt),
            ContextPrompt = Setting(global.ContextPrompt, stored?.ContextPrompt),
            NoContextRetrievedPrompt = Setting(global.NoContextRetrievedPrompt, stored?.NoContextRetrievedPrompt),
            TitlePrompt = Setting(global.TitlePrompt, stored?.TitlePrompt),
            CitationExtractionPrompt = Setting(global.CitationExtractionPrompt, stored?.CitationExtractionPrompt),
        },
    };

    private static void Apply(SubjectAiConfiguration target, SaveSubjectAiConfigurationRequest source)
    {
        target.ChunkingStrategy = source.ChunkingStrategy;
        target.ChunkSize = source.ChunkSize;
        target.ChunkOverlap = source.ChunkOverlap;
        target.EmbeddingModel = source.EmbeddingModel;
        target.TopK = source.TopK;
        target.SimilarityThreshold = source.SimilarityThreshold;
        target.MaxContextChunks = source.MaxContextChunks;
        target.LlmModel = source.LlmModel;
        target.ChatTemperature = source.ChatTemperature;
        target.MaxHistoryMessages = source.MaxHistoryMessages;
        target.ChatPrompt = source.ChatPrompt;
        target.ContextPrompt = source.ContextPrompt;
        target.NoContextRetrievedPrompt = source.NoContextRetrievedPrompt;
        target.TitleTemperature = source.TitleTemperature;
        target.TitlePrompt = source.TitlePrompt;
        target.CitationExtractionTemperature = source.CitationExtractionTemperature;
        target.CitationExtractionPrompt = source.CitationExtractionPrompt;
    }

    private static void Validate(SaveSubjectAiConfigurationRequest request, GlobalAiConfiguration global)
    {
        ValidateOptionalText(request.ChunkingStrategy, nameof(request.ChunkingStrategy));
        ValidateOptionalText(request.EmbeddingModel, nameof(request.EmbeddingModel));
        ValidateOptionalText(request.LlmModel, nameof(request.LlmModel));
        ValidateOptionalText(request.ChatPrompt, nameof(request.ChatPrompt));
        ValidateOptionalText(request.ContextPrompt, nameof(request.ContextPrompt));
        ValidateOptionalText(request.NoContextRetrievedPrompt, nameof(request.NoContextRetrievedPrompt));
        ValidateOptionalText(request.TitlePrompt, nameof(request.TitlePrompt));
        ValidateOptionalText(request.CitationExtractionPrompt, nameof(request.CitationExtractionPrompt));

        ValidateIndexing(
            request.ChunkingStrategy ?? global.ChunkingStrategy,
            request.ChunkSize ?? global.ChunkSize,
            request.ChunkOverlap ?? global.ChunkOverlap,
            request.EmbeddingModel ?? global.EmbeddingModel);

        ValidateAllowed(request.LlmModel ?? global.LlmModel, ChatModels, nameof(request.LlmModel), "chat model");
        ValidatePositive(request.TopK ?? global.TopK, nameof(request.TopK));
        ValidatePositive(request.MaxContextChunks ?? global.MaxContextChunks, nameof(request.MaxContextChunks));
        ValidatePositive(request.MaxHistoryMessages ?? global.MaxHistoryMessages, nameof(request.MaxHistoryMessages));
        ValidateRange(request.SimilarityThreshold ?? global.SimilarityThreshold, 0, 1, nameof(request.SimilarityThreshold));
        ValidateRange(request.ChatTemperature ?? global.ChatTemperature, 0, 2, nameof(request.ChatTemperature));
        ValidateRange(request.TitleTemperature ?? global.TitleTemperature, 0, 2, nameof(request.TitleTemperature));
        ValidateRange(request.CitationExtractionTemperature ?? global.CitationExtractionTemperature, 0, 2, nameof(request.CitationExtractionTemperature));
    }

    private static void ValidateIndexing(string strategy, int size, int overlap, string embeddingModel)
    {
        ValidateAllowed(strategy, Strategies, nameof(SaveSubjectAiConfigurationRequest.ChunkingStrategy), "chunking strategy");
        ValidateAllowed(embeddingModel, EmbeddingModels, nameof(SaveSubjectAiConfigurationRequest.EmbeddingModel), "embedding model");
        if (size is < 100 or > 8000) throw new EntityValidationException("Chunk size must be between 100 and 8000.", nameof(SaveSubjectAiConfigurationRequest.ChunkSize));
        if (overlap < 0 || overlap >= size) throw new EntityValidationException("Chunk overlap must be non-negative and smaller than chunk size.", nameof(SaveSubjectAiConfigurationRequest.ChunkOverlap));
    }

    private static void ValidateOptionalText(string? value, string property)
    {
        if (value != null && string.IsNullOrWhiteSpace(value)) throw new EntityValidationException($"{property} cannot be empty.", property);
    }

    private static void ValidateAllowed(string value, string[] allowed, string property, string description)
    {
        if (!allowed.Contains(value, StringComparer.Ordinal)) throw new EntityValidationException($"Unknown {description} '{value}'.", property);
    }

    private static void ValidatePositive(int value, string property)
    {
        if (value <= 0) throw new EntityValidationException($"{property} must be positive.", property);
    }

    private static void ValidateRange(double value, double minimum, double maximum, string property)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum) throw new EntityValidationException($"{property} must be between {minimum} and {maximum}.", property);
    }

    private static void ValidateRange(float value, float minimum, float maximum, string property)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum) throw new EntityValidationException($"{property} must be between {minimum} and {maximum}.", property);
    }

    private static AiOptionDto Option(string value, string label) => new() { Value = value, Label = label };
    private static AiStringSettingDto Setting(string global, string? stored) => new() { GlobalDefault = global, StoredOverride = stored, EffectiveValue = stored ?? global };
    private static AiIntSettingDto Setting(int global, int? stored) => new() { GlobalDefault = global, StoredOverride = stored, EffectiveValue = stored ?? global };
    private static AiDoubleSettingDto Setting(double global, double? stored) => new() { GlobalDefault = global, StoredOverride = stored, EffectiveValue = stored ?? global };
    private static AiFloatSettingDto Setting(float global, float? stored) => new() { GlobalDefault = global, StoredOverride = stored, EffectiveValue = stored ?? global };
}
