using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;

namespace Business.Services.AI;

public class AiConfigurationResolver(IUnitOfWork unitOfWork) : IAiConfigurationResolver
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<EffectiveAiConfiguration> GetAiConfigurationAsync(int? subjectId, CancellationToken cxlTkn)
    {
        var subjectConfig = subjectId.HasValue
            ? (await _unitOfWork.SubjectAiConfigurations.GetAsync(
                filter: e => e.Id == subjectId,
                cancellationToken: cxlTkn))
                .FirstOrDefault()
            : null;

        var globalConfig = (await _unitOfWork.GlobalAiConfigurations.GetAsync(cancellationToken: cxlTkn)).FirstOrDefault()
                           ?? throw new InvalidOperationException("Global AI configuration has not been initialized.");

        return new EffectiveAiConfiguration
        {
            ChunkingStrategy =
                subjectConfig?.ChunkingStrategy
                ?? globalConfig.ChunkingStrategy,

            EmbeddingModel =
                subjectConfig?.EmbeddingModel
                ?? globalConfig.EmbeddingModel,

            TopK =
                subjectConfig?.TopK
                ?? globalConfig.TopK,

            SimilarityThreshold =
                subjectConfig?.SimilarityThreshold
                ?? globalConfig.SimilarityThreshold,

            LlmModel =
                subjectConfig?.LlmModel
                ?? globalConfig.LlmModel,

            ChatTemperature =
                subjectConfig?.ChatTemperature
                ?? globalConfig.ChatTemperature,

            ChatPrompt =
                subjectConfig?.ChatPrompt
                ?? globalConfig.ChatPrompt,

            ContextPrompt =
                subjectConfig?.ContextPrompt
                ?? globalConfig.ContextPrompt,

            NoContextRetrievedPrompt =
                subjectConfig?.NoContextRetrievedPrompt
                ?? globalConfig.NoContextRetrievedPrompt,

            TitleTemperature =
                subjectConfig?.TitleTemperature
                ?? globalConfig.TitleTemperature,

            TitlePrompt =
                subjectConfig?.TitlePrompt
                ?? globalConfig.TitlePrompt,

            CitationExtractionTemperature =
                subjectConfig?.CitationExtractionTemperature
                ?? globalConfig.CitationExtractionTemperature,

            CitationExtractionPrompt =
                subjectConfig?.CitationExtractionPrompt
                ?? globalConfig.CitationExtractionPrompt,

            MaxContextChunks =
                subjectConfig?.MaxContextChunks
                ?? globalConfig.MaxContextChunks,

            MaxHistoryMessages =
                subjectConfig?.MaxHistoryMessages
                ?? globalConfig.MaxHistoryMessages,
        };
    }
}
