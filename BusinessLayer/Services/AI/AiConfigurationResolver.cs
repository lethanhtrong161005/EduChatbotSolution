using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;

namespace Business.Services.AI;

public class AiConfigurationResolver(IUnitOfWork unitOfWork) : IAiConfigurationResolver
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<EffectiveAiConfiguration> GetAiConfigurationAsync(int subjectId, CancellationToken cxlTkn)
    {
        var subjectConfig = (await _unitOfWork.SubjectAiConfigurations.GetAsync(
            filter: e => e.Id == subjectId,
            cancellationToken: cxlTkn))
            .FirstOrDefault();

        var globalConfig = (await _unitOfWork.GlobalAiConfigurations.GetAsync(cancellationToken: cxlTkn))
            .FirstOrDefault()
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

            Temperature =
                subjectConfig?.Temperature
                ?? globalConfig.Temperature,

            SystemPrompt =
                subjectConfig?.SystemPrompt
                ?? globalConfig.SystemPrompt,

            MaxContextChunks =
                subjectConfig?.MaxContextChunks
                ?? globalConfig.MaxContextChunks,

            MaxHistoryMessages =
                subjectConfig?.MaxHistoryMessages
                ?? globalConfig.MaxHistoryMessages,
        };
    }
}
