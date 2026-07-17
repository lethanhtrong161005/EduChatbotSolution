using Business.Services.AI;
using DataAccess.Data;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests;

public class AnswerReconstructionServiceTests
{
    [Test]
    public async Task ReconstructChatAssistantAsync_OwnerGetsPersistedScopeRequestEvidenceAndAnswer()
    {
        var ownerId = Guid.NewGuid();
        var message = new ChatMessage { Id = Guid.NewGuid(), ChatSessionId = Guid.NewGuid(), ChatRole = ChatRole.Assistant, ChatSession = new ChatSession { UserId = ownerId }, ReconstructionCompleteness = ReconstructionCompleteness.Complete, RawContent = "raw", Content = "final" };
        message.RequestMessages.Add(new ChatMessageRequestMessage { MessageOrder = 2, Role = "user", Content = "question" });
        message.RequestMessages.Add(new ChatMessageRequestMessage { MessageOrder = 1, Role = "system", Content = "system" });
        message.ResolvedSubjects.Add(new ChatMessageSubjectSnapshot { SubjectOrder = 1, SourceSubjectId = 7, SubjectCode = "DB201", SubjectName = "Database" });
        message.RetrievedContexts.Add(new ChatMessageContext { RetrievalRank = 1, PromptOrder = 1, WasIncludedInPrompt = true, ChunkText = "evidence" });
        var (sut, repository) = Service();
        repository.Setup(e => e.GetChatAssistantAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);

        var result = await sut.ReconstructChatAssistantAsync(message.Id, ownerId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Completeness, Is.EqualTo(ReconstructionCompleteness.Complete));
            Assert.That(result.RequestMessages.Select(e => e.MessageOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.ResolvedSubjects.Single().SourceSubjectId, Is.EqualTo(7));
            Assert.That(result.Retrievals.Single().ChunkText, Is.EqualTo("evidence"));
            Assert.That(result.RawAnswer, Is.EqualTo("raw"));
            Assert.That(result.FinalAnswer, Is.EqualTo("final"));
        }
    }

    [Test]
    public void ReconstructChatAssistantAsync_NonOwnerIsHiddenAsNotFound()
    {
        var message = new ChatMessage { Id = Guid.NewGuid(), ChatRole = ChatRole.Assistant, ChatSession = new ChatSession { UserId = Guid.NewGuid() } };
        var (sut, repository) = Service();
        repository.Setup(e => e.GetChatAssistantAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        Assert.That(async () => await sut.ReconstructChatAssistantAsync(message.Id, Guid.NewGuid()), Throws.TypeOf<EntityNotFoundException>());
    }

    [Test]
    public async Task ReconstructExperimentResponse_ReturnsImmutableQuestionAndEveryEvaluationAttempt()
    {
        var response = new TestResponse
        {
            Id = Guid.NewGuid(), ExperimentId = Guid.NewGuid(), Experiment = new Experiment { ConfigurationSnapshot = new ExperimentConfigurationSnapshot() }, ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            DatasetName = "DB201 Vietnamese 50", DatasetKey = "db201-vi-50-v1", DatasetVersion = "1", QuestionExternalId = "DB201-VI-001", Question = "question", GroundTruth = "reference", GeneratedAnswer = "answer",
        };
        response.RequestMessages.Add(new TestResponseRequestMessage { MessageOrder = 1, Role = "user", Content = "question" });
        response.RetrievedContexts.Add(new TestResponseContext { RetrievalRank = 1, PromptOrder = 1, WasIncludedInPrompt = true, ChunkText = "context" });
        var first = new TestResponseEvaluationAttempt { Id = Guid.NewGuid(), AttemptNumber = 1, Status = EvaluationAttemptStatus.Completed, EvaluatorFamily = "legacy" };
        var second = new TestResponseEvaluationAttempt { Id = Guid.NewGuid(), AttemptNumber = 2, Status = EvaluationAttemptStatus.PartiallyCompleted, EvaluatorFamily = "python-ragas" };
        second.Metrics.Add(new TestResponseEvaluationMetric { MetricName = RagasMetricName.Faithfulness, Status = EvaluationMetricStatus.Completed, Score = .8 });
        response.EvaluationAttempts.Add(first); response.EvaluationAttempts.Add(second); response.CurrentEvaluationAttemptId = second.Id;
        var (sut, repository) = Service();
        repository.Setup(e => e.GetExperimentResponseAsync(response.Id, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await sut.ReconstructExperimentResponseAsync(response.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Question, Is.EqualTo("question"));
            Assert.That(result.GroundTruth, Is.EqualTo("reference"));
            Assert.That(result.Retrievals.Single().ChunkText, Is.EqualTo("context"));
            Assert.That(result.EvaluationAttempts.Select(e => e.AttemptNumber), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.EvaluationAttempts.Single(e => e.IsCurrent).Metrics.Single().Score, Is.EqualTo(.8));
        }
    }

    private static (AnswerReconstructionService Service, Mock<AnswerReconstructionRepository> Repository) Service()
    {
        var context = new EduChatAiDbContext(new DbContextOptionsBuilder<EduChatAiDbContext>().UseNpgsql("Host=localhost;Database=reconstruction_model_only;Username=unused;Password=unused", npgsql => npgsql.UseVector()).Options);
        var repository = new Mock<AnswerReconstructionRepository>(context);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(e => e.AnswerReconstructions).Returns(repository.Object);
        return (new AnswerReconstructionService(unitOfWork.Object), repository);
    }
}
