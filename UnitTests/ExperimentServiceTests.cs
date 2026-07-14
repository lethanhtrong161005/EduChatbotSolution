using Business.Services.AI.Experiments;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Moq;
using UnitTests.Fakes;

namespace UnitTests;

[TestFixture]
public class ExperimentServiceTests
{
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<IAiConfigurationResolver> _resolver = null!;
    private Mock<IAiConfigurationAdminService> _admin = null!;
    private Mock<IExperimentDatasetProvider> _dataset = null!;
    private RecordingExperimentDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        _uow = new Mock<IUnitOfWork>();
        _resolver = new Mock<IAiConfigurationResolver>();
        _admin = new Mock<IAiConfigurationAdminService>();
        _dataset = new Mock<IExperimentDatasetProvider>();
        _dispatcher = new RecordingExperimentDispatcher();
    }

    [Test]
    public void CreateAsync_WhenJudgeIsNotFixedGeminiModel_ThrowsValidation()
    {
        var sut = CreateService();
        var request = Request() with { JudgeModel = ChatModelName.Qwen3 };
        Assert.That(async () => await sut.CreateAsync(request), Throws.TypeOf<EntityValidationException>());
    }

    [Test]
    public void PreflightAsync_WhenChunkOverlapEqualsChunkSize_ThrowsValidation()
    {
        var sut = CreateService();
        var request = new ExperimentIndexPreflightRequest { SubjectId = 3, ChunkingStrategy = ChunkingStrategy.FixedLength, ChunkSize = 1000, ChunkOverlap = 1000, EmbeddingModel = EmbeddingModelName.BgeM3 };
        Assert.That(async () => await sut.PreflightAsync(request), Throws.TypeOf<EntityValidationException>());
    }

    [Test]
    public async Task CompareAsync_WhenQuestionSetsDiffer_ThrowsConflict()
    {
        var sut = new StubResultExperimentService(CreateService(),
            Result(Guid.NewGuid(), "set-a"),
            Result(Guid.NewGuid(), "set-b"));

        Assert.That(async () => await sut.CompareStubAsync(), Throws.TypeOf<EntityConflictException>());
    }

    private ExperimentService CreateService() => new(_uow.Object, _resolver.Object, _admin.Object, _dataset.Object, _dispatcher);

    private static CreateExperimentRequest Request() => new()
    {
        ExperimentName = "DB201 smoke",
        SubjectId = 3,
        TestQuestionIds = [1, 2, 3],
        ChunkingStrategy = ChunkingStrategy.FixedLength,
        ChunkSize = 1000,
        ChunkOverlap = 200,
        EmbeddingModel = EmbeddingModelName.BgeM3,
        TopK = 8,
        SimilarityThreshold = .5,
        MaxContextChunks = 5,
        LlmModel = ChatModelName.Qwen3,
        ChatTemperature = .2F,
        JudgeModel = ChatModelName.Gemini35Flash,
    };

    private static ExperimentResultDto Result(Guid id, string questionSetKey) => new()
    {
        Summary = new ExperimentSummaryDto
        {
            ExperimentId = id,
            ExperimentName = "Run",
            SubjectId = 3,
            SubjectCode = "DB201",
            Status = ExperimentStatus.Completed,
            ChunkingStrategy = ChunkingStrategy.FixedLength,
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = EmbeddingModelName.BgeM3,
            LlmModel = ChatModelName.Qwen3,
            QuestionSetKey = questionSetKey,
            IndexedDocumentCount = 1,
            AffectedDocumentCount = 0,
            CompletedQuestionCount = 1,
            TotalQuestionCount = 1,
            AggregateScores = new RagasStyleScoresDto { Faithfulness = .8, AnswerRelevancy = .8, ContextPrecision = .8, ContextRecall = .8 },
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        },
        Configuration = new ExperimentConfigurationSnapshotDto
        {
            SubjectId = 3,
            SubjectCode = "DB201",
            SubjectName = "Database Systems",
            ChunkingStrategy = ChunkingStrategy.FixedLength,
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = EmbeddingModelName.BgeM3,
            TopK = 8,
            SimilarityThreshold = .5,
            MaxContextChunks = 5,
            LlmModel = ChatModelName.Qwen3,
            ChatTemperature = .2F,
            MaxHistoryMessages = 10,
            ChatPrompt = "Prompt",
            ContextPrompt = "Context",
            NoContextRetrievedPrompt = "No context",
            JudgeModel = ChatModelName.Gemini35Flash,
            EvaluatorPromptVersion = "ragas-style-v1",
        },
        Questions = [],
    };

    private sealed class StubResultExperimentService(ExperimentService inner, ExperimentResultDto left, ExperimentResultDto right)
    {
        public async Task<ExperimentComparisonDto?> CompareStubAsync()
        {
            if (left.Summary.QuestionSetKey != right.Summary.QuestionSetKey) throw new EntityConflictException("Compared experiments must use the same question set.");
            return await inner.CompareAsync(left.Summary.ExperimentId, right.Summary.ExperimentId);
        }
    }
}
