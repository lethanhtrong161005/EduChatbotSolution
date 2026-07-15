using AutoMapper;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Presentation.Mappings;

public class ExperimentMappingProfile : Profile
{
    public ExperimentMappingProfile()
    {
        CreateMap<Experiment, ExperimentSummaryDto>()
            .ForMember(d => d.ExperimentId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.SubjectCode, o => o.MapFrom(s => s.Subject.Code))
            .ForMember(d => d.ChunkingStrategy, o => o.MapFrom(s => s.ConfigurationSnapshot.ChunkingStrategy))
            .ForMember(d => d.ChunkSize, o => o.MapFrom(s => s.ConfigurationSnapshot.ChunkSize))
            .ForMember(d => d.ChunkOverlap, o => o.MapFrom(s => s.ConfigurationSnapshot.ChunkOverlap))
            .ForMember(d => d.EmbeddingModel, o => o.MapFrom(s => s.ConfigurationSnapshot.EmbeddingModel))
            .ForMember(d => d.LlmModel, o => o.MapFrom(s => s.ConfigurationSnapshot.LlmModel))
            .ForMember(d => d.AggregateScores, o => o.MapFrom(s => Scores(s.Faithfulness, s.AnswerRelevancy, s.ContextPrecision, s.ContextRecall)));

        CreateMap<ExperimentConfigurationSnapshot, ExperimentConfigurationSnapshotDto>();

        CreateMap<TestResponse, ExperimentQuestionResultDto>()
            .ForMember(d => d.TestResponseId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TestQuestionId, o => o.MapFrom(s => s.TestQuestionId))
            .ForMember(d => d.ExternalId, o => o.MapFrom(s => s.TestQuestion.ExternalId))
            .ForMember(d => d.Question, o => o.MapFrom(s => s.TestQuestion.Question))
            .ForMember(d => d.GroundTruth, o => o.MapFrom(s => s.TestQuestion.GroundTruth))
            .ForMember(d => d.RetrievedContexts, o => o.MapFrom(s => s.RetrievedContexts.OrderBy(e => e.ContextIndex).Select(e => e.ContextText)))
            .ForMember(d => d.Scores, o => o.MapFrom(s => Scores(s.Faithfulness, s.AnswerRelevancy, s.ContextPrecision, s.ContextRecall)));

        CreateMap<ChatGenerationMetrics, TestResponse>(MemberList.None);
        CreateMap<RagasStyleEvaluationResult, TestResponse>(MemberList.None);
    }

    private static RagasStyleScoresDto Scores(double? faithfulness, double? answerRelevancy, double? contextPrecision, double? contextRecall) =>
        new() { Faithfulness = faithfulness, AnswerRelevancy = answerRelevancy, ContextPrecision = contextPrecision, ContextRecall = contextRecall };
}
