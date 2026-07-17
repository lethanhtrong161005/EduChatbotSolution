using AutoMapper;
using Business.Services.AI.Experiments;
using DataAccess.Data;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using UnitTests.Fakes;

namespace UnitTests;

[TestFixture]
public class ExperimentServiceTests
{
    private Mock<IMapper> _mapper = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<IAiConfigurationResolver> _resolver = null!;
    private Mock<IAiConfigurationAdminService> _admin = null!;
    private Mock<IExperimentDatasetProvider> _dataset = null!;
    private Mock<IPythonRagasClient> _ragas = null!;
    private RecordingExperimentDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        _mapper = new Mock<IMapper>();
        _uow = new Mock<IUnitOfWork>();
        _resolver = new Mock<IAiConfigurationResolver>();
        _admin = new Mock<IAiConfigurationAdminService>();
        _dataset = new Mock<IExperimentDatasetProvider>();
        _ragas = new Mock<IPythonRagasClient>();
        _ragas.Setup(e => e.GetCapabilitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Capabilities());
        _dispatcher = new RecordingExperimentDispatcher();
    }

    [Test]
    public async Task CreateAsync_AcceptsSelectedEvaluatorAndCapturesCitationExtractionSettings()
    {
        Experiment? captured = null;
        ConfigureCreateDependencies(experiment => captured = experiment);
        var sut = CreateService();

        await sut.CreateAsync(Request() with { TestQuestionIds = [1] });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.ConfigurationSnapshot.EvaluatorLlmModel, Is.EqualTo(ChatModelName.Gemini35Flash));
            Assert.That(captured.ConfigurationSnapshot.EvaluatorEmbeddingModel, Is.EqualTo(EmbeddingModelName.GeminiEmbedding2));
            Assert.That(captured.ConfigurationSnapshot.CitationExtractionTemperature, Is.EqualTo(.15F));
            Assert.That(captured.ConfigurationSnapshot.CitationExtractionPrompt, Is.EqualTo("Extract grounded citations."));
        }
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
        var left = CompletedExperiment(Guid.NewGuid(), "set-a");
        var right = CompletedExperiment(Guid.NewGuid(), "set-b");
        ConfigureExperimentResults(left, right);
        var sut = CreateService();

        Assert.That(async () => await sut.CompareAsync(left.Id, right.Id), Throws.TypeOf<EntityConflictException>());
    }

    private ExperimentService CreateService() => new(_uow.Object, _resolver.Object, _admin.Object, _dataset.Object, _dispatcher, _ragas.Object, _mapper.Object);

    private void ConfigureCreateDependencies(Action<Experiment> capture)
    {
        var context = new Mock<DbContext>();
        var subjects = new Mock<GenericRepository<Subject>>(context.Object);
        subjects.Setup(item => item.FindByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject { Id = 3, Code = "DB201", Name = "Database Systems" });
        _uow.Setup(item => item.Subjects).Returns(subjects.Object);

        var questions = new Mock<GenericRepository<TestQuestion>>(context.Object);
        questions.Setup(item => item.GetAsync(
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<TestQuestion, bool>>>(),
                It.IsAny<Func<IQueryable<TestQuestion>, IOrderedQueryable<TestQuestion>>>(),
                It.IsAny<(int, int)>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TestQuestion { Id = 1, SubjectId = 3, ExternalId = "DB201-VI-001", Question = "Question", GroundTruth = "Ground truth" }]);
        _uow.Setup(item => item.TestQuestions).Returns(questions.Object);

        var runContext = new EduChatAiDbContext(new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql("Host=localhost;Database=experiment_service_model_only;Username=unused;Password=unused", npgsql => npgsql.UseVector())
            .Options);
        var runs = new Mock<ExperimentRunRepository>(runContext);
        runs.Setup(item => item.CountAffectedDocumentsAsync(3, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        runs.Setup(item => item.CreateQueuedAsync(It.IsAny<Experiment>(), It.IsAny<CancellationToken>()))
            .Callback<Experiment, CancellationToken>((experiment, _) => capture(experiment))
            .ReturnsAsync((Experiment experiment, CancellationToken _) => experiment);
        _uow.Setup(item => item.ExperimentRuns).Returns(runs.Object);

        _dataset.Setup(item => item.ImportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(50);
        _dataset.Setup(item => item.GetDatasetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new TestDatasetDto { DatasetName = "DB201 Vietnamese 50", DatasetKey = "db201-vi-50-v1", DatasetVersion = "1", Language = "vi", SubjectCode = "DB201", Questions = [] });
        _resolver.Setup(item => item.GetAiConfigurationAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(Configuration());
    }

    private void ConfigureExperimentResults(params Experiment[] experiments)
    {
        var context = new Mock<DbContext>();
        var repository = new Mock<GenericRepository<Experiment>>(context.Object);
        repository.Setup(item => item.GetAsync(
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Experiment, bool>>>(),
                It.IsAny<Func<IQueryable<Experiment>, IOrderedQueryable<Experiment>>>(),
                It.IsAny<(int, int)>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((string[] _, Expression<Func<Experiment, bool>> filter, Func<IQueryable<Experiment>, IOrderedQueryable<Experiment>> _, (int, int) _, bool _, bool _, bool _, CancellationToken _) =>
                Task.FromResult<IEnumerable<Experiment>>(experiments.Where(filter.Compile()).ToArray()));
        _uow.Setup(item => item.Experiments).Returns(repository.Object);
        _mapper.Setup(item => item.Map<ExperimentSummaryDto>(It.IsAny<Experiment>())).Returns((Experiment source) => Summary(source));
        _mapper.Setup(item => item.Map<ExperimentConfigurationSnapshotDto>(It.IsAny<ExperimentConfigurationSnapshot>())).Returns(SnapshotDto());
    }

    private static EffectiveAiConfiguration Configuration() => new()
    {
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
        ChatPrompt = "Answer using context.",
        ContextPrompt = "Context:",
        NoContextRetrievedPrompt = "No context.",
        TitleTemperature = .2F,
        TitlePrompt = "Generate a title.",
        CitationExtractionTemperature = .15F,
        CitationExtractionPrompt = "Extract grounded citations.",
    };

    private static Experiment CompletedExperiment(Guid id, string questionSetKey) => new()
    {
        Id = id,
        ExperimentName = "Run",
        SubjectId = 3,
        Subject = new Subject { Id = 3, Code = "DB201", Name = "Database Systems" },
        Status = ExperimentStatus.Completed,
        QuestionSetKey = questionSetKey,
        ConfigurationSnapshot = new ExperimentConfigurationSnapshot { Id = id },
    };

    private static ExperimentSummaryDto Summary(Experiment experiment) => new()
    {
        ExperimentId = experiment.Id,
        ExperimentName = experiment.ExperimentName,
        SubjectId = experiment.SubjectId,
        SubjectCode = experiment.Subject.Code,
        Status = experiment.Status,
        ChunkingStrategy = ChunkingStrategy.FixedLength,
        ChunkSize = 1000,
        ChunkOverlap = 200,
        EmbeddingModel = EmbeddingModelName.BgeM3,
        LlmModel = ChatModelName.Qwen3,
        QuestionSetKey = experiment.QuestionSetKey,
        IndexedDocumentCount = 0,
        AffectedDocumentCount = 0,
        CompletedQuestionCount = 0,
        TotalQuestionCount = 0,
        AggregateScores = new RagasStyleScoresDto { Faithfulness = null, AnswerRelevancy = null, ContextPrecision = null, ContextRecall = null },
        CreatedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow,
    };

    private static ExperimentConfigurationSnapshotDto SnapshotDto() => Result(Guid.NewGuid(), "unused").Configuration;

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
        EvaluatorLlmProvider = "gemini", EvaluatorLlmModel = ChatModelName.Gemini35Flash,
        EvaluatorEmbeddingProvider = "gemini", EvaluatorEmbeddingModel = EmbeddingModelName.GeminiEmbedding2,
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
            CitationExtractionTemperature = 0,
            CitationExtractionPrompt = "Extract citations",
            EvaluatorLlmProvider = "gemini", EvaluatorLlmModel = ChatModelName.Gemini35Flash,
            EvaluatorEmbeddingProvider = "gemini", EvaluatorEmbeddingModel = EmbeddingModelName.GeminiEmbedding2,
            EvaluatorMetricSetKey = "ragas-rag-core-v1", EvaluatorPromptVersion = "vi-ragas-v1",
        },
        Questions = [],
    };

    private static PythonRagasCapabilities Capabilities() => new()
    {
        ContractVersion = ExperimentEvaluationService.ContractVersion, ServiceVersion = "test", RagasVersion = "0.4.3", PromptVersion = "vi-ragas-v1", Metrics = RagasMetricName.All,
        LlmOptions = [new PythonRagasModelOption { Provider = "gemini", Model = ChatModelName.Gemini35Flash, Label = "Gemini" }],
        EmbeddingOptions = [new PythonRagasModelOption { Provider = "gemini", Model = EmbeddingModelName.GeminiEmbedding2, Label = "Gemini Embedding" }],
    };

}
