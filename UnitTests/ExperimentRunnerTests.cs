using AutoMapper;
using Business.Services.AI.Experiments;
using DataAccess.Data;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

[TestFixture]
public sealed class ExperimentRunnerTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ExperimentRunRepository> _runs = null!;
    private Mock<IAiConfigurationResolver> _configuration = null!;
    private Mock<ISubjectReindexCoordinator> _reindex = null!;
    private Mock<IChatGenerationService> _generation = null!;
    private Mock<IExperimentEvaluationService> _evaluation = null!;
    private Mock<IMapper> _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _runs = new Mock<ExperimentRunRepository>(new EduChatAiDbContext(new DbContextOptionsBuilder<EduChatAiDbContext>().UseNpgsql("Host=localhost;Database=runner_model_only;Username=unused;Password=unused", npgsql => npgsql.UseVector()).Options));
        _configuration = new Mock<IAiConfigurationResolver>();
        _reindex = new Mock<ISubjectReindexCoordinator>();
        _generation = new Mock<IChatGenerationService>();
        _evaluation = new Mock<IExperimentEvaluationService>();
        _mapper = new Mock<IMapper>();
        _unitOfWork.SetupGet(e => e.ExperimentRuns).Returns(_runs.Object);
        _unitOfWork.Setup(e => e.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _configuration.Setup(e => e.GetAiConfigurationAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Configuration());
        _mapper.Setup(e => e.Map(It.IsAny<ChatGenerationMetrics>(), It.IsAny<TestResponse>())).Returns((ChatGenerationMetrics _, TestResponse response) => response);
    }

    [Test]
    public async Task RunAsync_CompatibleAnswerAndPartialEvaluation_CompletesWithoutReindexAndPersistsSnapshots()
    {
        var experiment = Experiment();
        ConfigureRun(experiment, false, 0);
        _generation.Setup(e => e.GenerateAnswerAsync(It.IsAny<ChatGenerationRequest>(), It.IsAny<Func<string, Task>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Generation());
        ConfigureEvaluation(experiment.TestResponses.Single(), EvaluationAttemptStatus.PartiallyCompleted, true);

        await Runner().RunAsync(experiment.Id);

        var response = experiment.TestResponses.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(experiment.Status, Is.EqualTo(ExperimentStatus.Completed));
            Assert.That(experiment.FailureReason, Is.Null);
            Assert.That(response.Status, Is.EqualTo(ExperimentQuestionStatus.Completed));
            Assert.That(response.ReconstructionCompleteness, Is.EqualTo(ReconstructionCompleteness.Complete));
            Assert.That(response.RawGeneratedAnswer, Is.EqualTo("raw answer"));
            Assert.That(response.GeneratedAnswer, Is.EqualTo("final answer"));
            Assert.That(response.RetrievedContexts.Select(e => e.RetrievalRank), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(response.RetrievedContexts.Where(e => e.WasIncludedInPrompt).Select(e => e.PromptOrder), Is.EqualTo(new int?[] { 1 }));
            Assert.That(response.RequestMessages.Select(e => e.MessageOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(response.CurrentEvaluationAttempt!.Status, Is.EqualTo(EvaluationAttemptStatus.PartiallyCompleted));
        }

        _reindex.Verify(e => e.RunAsync(It.IsAny<int>(), It.IsAny<EffectiveAiConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        _evaluation.Verify(e => e.AppendInitialEvaluationAsync(response.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_IncompatibleRun_ReindexesBeforeGeneration()
    {
        var experiment = Experiment();
        ConfigureRun(experiment, true, 3);
        var reindexed = false;
        _reindex.Setup(e => e.RunAsync(experiment.SubjectId, It.IsAny<EffectiveAiConfiguration>(), It.IsAny<CancellationToken>())).Callback(() => reindexed = true).Returns(Task.CompletedTask);
        _generation.Setup(e => e.GenerateAnswerAsync(It.IsAny<ChatGenerationRequest>(), It.IsAny<Func<string, Task>>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => { Assert.That(reindexed, Is.True); return Generation(); });
        ConfigureEvaluation(experiment.TestResponses.Single(), EvaluationAttemptStatus.Completed, true);

        await Runner().RunAsync(experiment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(experiment.Status, Is.EqualTo(ExperimentStatus.Completed));
            Assert.That(experiment.IndexedDocumentCount, Is.EqualTo(3));
            Assert.That(reindexed, Is.True);
        }
    }

    [Test]
    public async Task RunAsync_NoUsableEvaluationResult_FailsExperimentButKeepsCompletedAnswer()
    {
        var experiment = Experiment();
        ConfigureRun(experiment, false, 0);
        _generation.Setup(e => e.GenerateAnswerAsync(It.IsAny<ChatGenerationRequest>(), It.IsAny<Func<string, Task>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Generation());
        ConfigureEvaluation(experiment.TestResponses.Single(), EvaluationAttemptStatus.Failed, false);

        await Runner().RunAsync(experiment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(experiment.Status, Is.EqualTo(ExperimentStatus.Failed));
            Assert.That(experiment.FailureReason, Is.EqualTo("No experiment question obtained a usable evaluation result."));
            Assert.That(experiment.TestResponses.Single().Status, Is.EqualTo(ExperimentQuestionStatus.Completed));
            Assert.That(experiment.TestResponses.Single().GeneratedAnswer, Is.EqualTo("final answer"));
        }
    }

    private ExperimentRunner Runner() => new(_unitOfWork.Object, _configuration.Object, _reindex.Object, _generation.Object, _evaluation.Object, _mapper.Object, Mock.Of<ILogger<ExperimentRunner>>());

    private void ConfigureRun(Experiment experiment, bool requiresReindex, int affected)
    {
        _runs.Setup(e => e.GetForRunAsync(experiment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(experiment);
        _runs.Setup(e => e.PrepareAsync(experiment.Id, It.IsAny<SubjectAiConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(() =>
        {
            experiment.Status = requiresReindex ? ExperimentStatus.PreparingIndex : ExperimentStatus.Running;
            return new ExperimentRunPreparation(requiresReindex, affected);
        });
    }

    private void ConfigureEvaluation(TestResponse response, EvaluationAttemptStatus status, bool usable)
    {
        _evaluation.Setup(e => e.AppendInitialEvaluationAsync(response.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() =>
        {
            var attempt = new TestResponseEvaluationAttempt { Id = Guid.NewGuid(), TestResponseId = response.Id, AttemptNumber = 1, Status = status, EvaluatorFamily = "python-ragas" };
            attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = RagasMetricName.Faithfulness, Status = usable ? EvaluationMetricStatus.Completed : EvaluationMetricStatus.Failed, Score = usable ? .8 : null });
            response.EvaluationAttempts.Add(attempt); response.CurrentEvaluationAttemptId = attempt.Id; response.CurrentEvaluationAttempt = attempt;
            return attempt;
        });
    }

    private static Experiment Experiment()
    {
        var id = Guid.NewGuid();
        var experiment = new Experiment
        {
            Id = id, ExperimentName = "Runner test", SubjectId = 7, Status = ExperimentStatus.Queued, QuestionSetKey = "set", TotalQuestionCount = 1, ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            ConfigurationSnapshot = new ExperimentConfigurationSnapshot
            {
                Id = id, SubjectId = 7, SubjectCode = "DB201", SubjectName = "Database", ChunkingStrategy = ChunkingStrategy.FixedLength, ChunkSize = 1000, ChunkOverlap = 200,
                EmbeddingProvider = "ollama", EmbeddingModel = EmbeddingModelName.BgeM3, TopK = 2, SimilarityThreshold = .5, MaxContextChunks = 1, LlmProvider = "ollama", LlmModel = ChatModelName.Qwen3,
                ChatTemperature = .2F, MaxHistoryMessages = 10, ChatPrompt = "System", ContextPrompt = "Context", NoContextRetrievedPrompt = "No context", CitationExtractionTemperature = 0, CitationExtractionPrompt = "Citations",
                EvaluatorLlmProvider = "ollama", EvaluatorLlmModel = "qwen3", EvaluatorEmbeddingProvider = "ollama", EvaluatorEmbeddingModel = "bge-m3", EvaluatorMetricSetKey = "ragas-rag-core-v1", EvaluatorPromptVersion = "vi-ragas-v1",
            },
        };
        experiment.TestResponses.Add(new TestResponse { Id = Guid.NewGuid(), ExperimentId = id, Status = ExperimentQuestionStatus.Pending, ReconstructionCompleteness = ReconstructionCompleteness.Complete, QuestionExternalId = "Q-001", Question = "Question", GroundTruth = "Reference" });
        return experiment;
    }

    private static EffectiveAiConfiguration Configuration() => new()
    {
        ChunkingStrategy = ChunkingStrategy.FixedLength, ChunkSize = 1000, ChunkOverlap = 200, EmbeddingModel = EmbeddingModelName.BgeM3, TopK = 2, SimilarityThreshold = .5, LlmModel = ChatModelName.Qwen3,
        ChatTemperature = .2F, ChatPrompt = "System", ContextPrompt = "Context", NoContextRetrievedPrompt = "No context", TitleTemperature = .2F, TitlePrompt = "Title",
        CitationExtractionTemperature = 0, CitationExtractionPrompt = "Citations", MaxContextChunks = 1, MaxHistoryMessages = 10,
    };

    private static ChatGenerationResult Generation() => new()
    {
        Answer = "final answer", RawAnswer = "raw answer", ChunkRetrievals = [], ChunkUsages = [],
        RequestMessages = [new NormalizedRequestMessageSnapshot { MessageOrder = 1, Role = "system", Content = "System" }, new NormalizedRequestMessageSnapshot { MessageOrder = 2, Role = "user", Content = "Question" }],
        RetrievedContexts =
        [
            new RetrievedContextSnapshot { RetrievalRank = 1, PromptOrder = 1, WasIncludedInPrompt = true, ChunkId = Guid.NewGuid(), ChunkText = "Prompt context", SimilarityScore = .9 },
            new RetrievedContextSnapshot { RetrievalRank = 2, PromptOrder = null, WasIncludedInPrompt = false, ChunkId = Guid.NewGuid(), ChunkText = "Retrieved only", SimilarityScore = .8 },
        ],
        Metrics = new ChatGenerationMetrics { PromptTokens = 12, CompletionTokens = 8, RetrievalTimeMs = 10, TimeToFirstTokenMs = 20, TotalResponseTimeMs = 30, TokensPerSecond = 20 },
    };
}
