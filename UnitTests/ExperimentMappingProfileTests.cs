using AutoMapper;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Presentation.Mappings;

namespace UnitTests;

public class ExperimentMappingProfileTests
{
    [Test]
    public void TestResponse_MapsIdentityOrderedContextsAndScores()
    {
        var configuration = new MapperConfiguration(
            config => config.AddProfile<ExperimentMappingProfile>(),
            NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();
        var responseId = Guid.NewGuid();
        var response = new TestResponse
        {
            Id = responseId,
            TestQuestionId = 42,
            TestQuestion = new TestQuestion
            {
                Id = 42,
                ExternalId = "DB201-VI-042",
                Question = "Question",
                GroundTruth = "Ground truth",
            },
            Faithfulness = .9,
            AnswerRelevancy = .8,
            ContextPrecision = .7,
            ContextRecall = .6,
        };
        response.RetrievedContexts.Add(new TestResponseContext { ContextIndex = 1, ContextText = "Second" });
        response.RetrievedContexts.Add(new TestResponseContext { ContextIndex = 0, ContextText = "First" });

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
}
