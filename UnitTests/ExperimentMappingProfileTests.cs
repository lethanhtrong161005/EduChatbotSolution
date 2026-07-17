using AutoMapper;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Presentation.Mappings;

namespace UnitTests;

public class ExperimentMappingProfileTests
{
    private static IMapper Mapper()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<ExperimentMappingProfile>(), NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }

    [Test]
    public void TestResponse_MapsIdentityOrderedContextsAndScores()
    {
        var mapper = Mapper();
        var responseId = Guid.NewGuid();
        var response = new TestResponse
        {
            Id = responseId,
            TestQuestionId = 42,
            QuestionExternalId = "DB201-VI-042",
            Question = "Question",
            GroundTruth = "Ground truth",
        };
        var attempt = new TestResponseEvaluationAttempt { Status = EvaluationAttemptStatus.Completed, EvaluatorProfileKey = "profile" };
        attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = Domain.Contracts.RagasMetricName.Faithfulness, Status = EvaluationMetricStatus.Completed, Score = .9 });
        attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = Domain.Contracts.RagasMetricName.AnswerRelevancy, Status = EvaluationMetricStatus.Completed, Score = .8 });
        attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = Domain.Contracts.RagasMetricName.ContextPrecision, Status = EvaluationMetricStatus.Completed, Score = .7 });
        attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = Domain.Contracts.RagasMetricName.ContextRecall, Status = EvaluationMetricStatus.Completed, Score = .6 });
        response.CurrentEvaluationAttempt = attempt;
        response.RetrievedContexts.Add(new TestResponseContext { RetrievalRank = 2, PromptOrder = 2, WasIncludedInPrompt = true, ChunkText = "Second" });
        response.RetrievedContexts.Add(new TestResponseContext { RetrievalRank = 1, PromptOrder = 1, WasIncludedInPrompt = true, ChunkText = "First" });

        var result = mapper.Map<ExperimentQuestionResultDto>(response);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TestResponseId, Is.EqualTo(responseId));
            Assert.That(result.TestQuestionId, Is.EqualTo(42));
            Assert.That(result.RetrievedContexts, Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(result.Scores.Faithfulness, Is.EqualTo(.9));
            Assert.That(result.Scores.AnswerRelevancy, Is.EqualTo(.8));
            Assert.That(result.Scores.ContextPrecision, Is.EqualTo(.7));
            Assert.That(result.Scores.ContextRecall, Is.EqualTo(.6));
        }
    }

    [Test]
    public void Experiment_MixedCurrentEvaluatorProfiles_MapsNullProfileAndMetricCoverage()
    {
        var experiment = new Experiment { Subject = new Subject { Code = "DB201" }, ConfigurationSnapshot = new ExperimentConfigurationSnapshot() };
        experiment.TestResponses.Add(Response("profile-a", .8));
        experiment.TestResponses.Add(Response("profile-b", .6));

        var result = Mapper().Map<ExperimentSummaryDto>(experiment);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.EvaluatorProfileKey, Is.Null);
            Assert.That(result.AggregateScores.Faithfulness, Is.EqualTo(.7));
            Assert.That(result.AggregateScores.Coverage.Faithfulness.SuccessfulCount, Is.EqualTo(2));
            Assert.That(result.AggregateScores.Coverage.Faithfulness.EligibleCount, Is.EqualTo(2));
        }
    }

    private static TestResponse Response(string profile, double score)
    {
        var response = new TestResponse();
        var attempt = new TestResponseEvaluationAttempt { Id = Guid.NewGuid(), EvaluatorProfileKey = profile };
        attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = RagasMetricName.Faithfulness, Status = EvaluationMetricStatus.Completed, Score = score });
        response.CurrentEvaluationAttempt = attempt; response.CurrentEvaluationAttemptId = attempt.Id;
        return response;
    }
}
