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
            .ForMember(d => d.EvaluatorProfileKey, o => o.MapFrom(s => EvaluatorProfile(s.TestResponses)))
            .ForMember(d => d.AggregateScores, o => o.MapFrom(s => Scores(s.TestResponses)));

        CreateMap<ExperimentConfigurationSnapshot, ExperimentConfigurationSnapshotDto>();

        CreateMap<TestResponse, ExperimentQuestionResultDto>()
            .ForMember(d => d.TestResponseId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TestQuestionId, o => o.MapFrom(s => s.TestQuestionId))
            .ForMember(d => d.ExternalId, o => o.MapFrom(s => s.QuestionExternalId))
            .ForMember(d => d.Question, o => o.MapFrom(s => s.Question))
            .ForMember(d => d.GroundTruth, o => o.MapFrom(s => s.GroundTruth))
            .ForMember(d => d.RetrievedContexts, o => o.MapFrom(s => s.RetrievedContexts.Where(e => e.WasIncludedInPrompt).OrderBy(e => e.PromptOrder).Select(e => e.ChunkText)))
            .ForMember(d => d.Explanation, o => o.MapFrom(s => s.CurrentEvaluationAttempt == null ? null : s.CurrentEvaluationAttempt.Summary))
            .ForMember(d => d.Scores, o => o.MapFrom(s => Scores(new[] { s })));

        CreateMap<ChatGenerationMetrics, TestResponse>(MemberList.None);
    }

    private static RagasStyleScoresDto Scores(IEnumerable<TestResponse> responses)
    {
        var eligible = responses.Where(e => e.CurrentEvaluationAttempt != null).ToArray();
        var faithfulness = Metric(eligible, RagasMetricName.Faithfulness);
        var answerRelevancy = Metric(eligible, RagasMetricName.AnswerRelevancy);
        var contextPrecision = Metric(eligible, RagasMetricName.ContextPrecision);
        var contextRecall = Metric(eligible, RagasMetricName.ContextRecall);
        return new RagasStyleScoresDto
        {
            Faithfulness = faithfulness.Score, AnswerRelevancy = answerRelevancy.Score, ContextPrecision = contextPrecision.Score, ContextRecall = contextRecall.Score,
            Coverage = new RagasStyleCoverageDto { Faithfulness = faithfulness.Coverage, AnswerRelevancy = answerRelevancy.Coverage, ContextPrecision = contextPrecision.Coverage, ContextRecall = contextRecall.Coverage },
        };
    }

    private static (double? Score, MetricCoverageDto Coverage) Metric(IReadOnlyCollection<TestResponse> responses, string metricName)
    {
        var values = responses.SelectMany(e => e.CurrentEvaluationAttempt!.Metrics).Where(e => e.MetricName == metricName && e.Status == EvaluationMetricStatus.Completed && e.Score.HasValue).Select(e => e.Score!.Value).ToArray();
        return (values.Length == 0 ? null : values.Average(), new MetricCoverageDto { SuccessfulCount = values.Length, EligibleCount = responses.Count });
    }

    private static string? EvaluatorProfile(IEnumerable<TestResponse> responses)
    {
        var profiles = responses.Select(e => e.CurrentEvaluationAttempt?.EvaluatorProfileKey).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.Ordinal).Take(2).ToArray();
        return profiles.Length == 1 ? profiles[0] : null;
    }
}
