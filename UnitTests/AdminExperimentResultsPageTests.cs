using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Pages.Admin.Experiments;

namespace UnitTests;

/// <summary>
/// Unit tests for <see cref="ResultsModel"/> and <see cref="CompareModel"/>.
/// Covers Admin authorization, Summaries/Result handlers, every experiment status,
/// exactly-two comparison selection, and comparison validation errors.
/// </summary>
[TestFixture]
public class AdminExperimentResultsPageTests
{
    private Mock<IExperimentService> _serviceMock = null!;
    private ResultsModel _resultsModel = null!;
    private CompareModel _compareModel = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IExperimentService>();

        var httpContext = new DefaultHttpContext();
        var pageContext = new PageContext { HttpContext = httpContext };

        _resultsModel = new ResultsModel(_serviceMock.Object);
        _resultsModel.PageContext = pageContext;

        _compareModel = new CompareModel(_serviceMock.Object);
        _compareModel.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
    }

    // ── Authorization ─────────────────────────────────────────

    [Test]
    public void ResultsModel_HasAdminAuthorizeAttribute()
    {
        var attrs = typeof(ResultsModel).GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true);
        Assert.That(attrs, Is.Not.Empty);
        var attr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)attrs[0];
        Assert.That(attr.Roles, Is.EqualTo("Admin"));
    }

    [Test]
    public void CompareModel_HasAdminAuthorizeAttribute()
    {
        var attrs = typeof(CompareModel).GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true);
        Assert.That(attrs, Is.Not.Empty);
        var attr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)attrs[0];
        Assert.That(attr.Roles, Is.EqualTo("Admin"));
    }

    // ── ResultsModel.OnGetAsync ───────────────────────────────

    [Test]
    public async Task OnGetAsync_NoId_DoesNotCallService()
    {
        await _resultsModel.OnGetAsync(null, CancellationToken.None);

        _serviceMock.VerifyNoOtherCalls();
        Assert.That(_resultsModel.InitialExperimentId, Is.Null);
    }

    [Test]
    public async Task OnGetAsync_WithId_SetsInitialExperimentId()
    {
        var id = Guid.NewGuid();

        await _resultsModel.OnGetAsync(id, CancellationToken.None);

        Assert.That(_resultsModel.InitialExperimentId, Is.EqualTo(id));
        _serviceMock.VerifyNoOtherCalls();
    }

    // ── ResultsModel.OnGetSummariesAsync ──────────────────────

    [Test]
    public async Task OnGetSummariesAsync_ReturnsSummariesFromService()
    {
        IReadOnlyList<ExperimentSummaryDto> summaries = new List<ExperimentSummaryDto>
        {
            BuildSummary(ExperimentStatus.Completed),
            BuildSummary(ExperimentStatus.Failed),
        }.AsReadOnly();

        _serviceMock.Setup(s => s.GetSummariesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(summaries);

        var result = await _resultsModel.OnGetSummariesAsync(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.SameAs(summaries));
    }

    // ── ResultsModel.OnGetResultAsync ─────────────────────────

    [Test]
    public async Task OnGetResultAsync_ExistingId_ReturnsResultDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildResult(id, ExperimentStatus.Completed);
        _serviceMock.Setup(s => s.GetResultAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _resultsModel.OnGetResultAsync(id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task OnGetResultAsync_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetResultAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExperimentResultDto?)null);

        var result = await _resultsModel.OnGetResultAsync(id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── Every experiment progress state ───────────────────────

    [TestCase(ExperimentStatus.Queued)]
    [TestCase(ExperimentStatus.PreparingIndex)]
    [TestCase(ExperimentStatus.Running)]
    [TestCase(ExperimentStatus.Evaluating)]
    [TestCase(ExperimentStatus.Completed)]
    [TestCase(ExperimentStatus.Failed)]
    public async Task OnGetResultAsync_AllStatusValues_ReturnedCorrectly(ExperimentStatus status)
    {
        var id = Guid.NewGuid();
        var dto = BuildResult(id, status);
        _serviceMock.Setup(s => s.GetResultAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _resultsModel.OnGetResultAsync(id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.InstanceOf<ExperimentResultDto>());
        var returned = (ExperimentResultDto)json.Value!;
        Assert.That(returned.Summary.Status, Is.EqualTo(status));
    }

    // ── CompareModel.OnGetAsync ───────────────────────────────

    [Test]
    public async Task CompareOnGetAsync_SetsInitialIds()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();

        await _compareModel.OnGetAsync(leftId, rightId, CancellationToken.None);

        Assert.That(_compareModel.InitialLeftId, Is.EqualTo(leftId));
        Assert.That(_compareModel.InitialRightId, Is.EqualTo(rightId));
        _serviceMock.VerifyNoOtherCalls();
    }

    // ── CompareModel.OnGetComparisonAsync — validation ────────

    [Test]
    public async Task OnGetComparisonAsync_EmptyLeftId_ReturnsBadRequest()
    {
        var result = await _compareModel.OnGetComparisonAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _serviceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OnGetComparisonAsync_EmptyRightId_ReturnsBadRequest()
    {
        var result = await _compareModel.OnGetComparisonAsync(Guid.NewGuid(), Guid.Empty, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _serviceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OnGetComparisonAsync_SameIdBothSides_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();

        var result = await _compareModel.OnGetComparisonAsync(id, id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _serviceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OnGetComparisonAsync_ServiceReturnsNull_Returns404()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExperimentComparisonDto?)null);

        var result = await _compareModel.OnGetComparisonAsync(leftId, rightId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task OnGetComparisonAsync_ValidPair_ReturnsComparisonDto()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();
        var dto = BuildComparison(leftId, rightId);
        _serviceMock.Setup(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _compareModel.OnGetComparisonAsync(leftId, rightId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.SameAs(dto));
    }

    // ── Comparison domain-exception translation ───────────────

    [Test]
    public async Task OnGetComparisonAsync_ServiceThrowsNotFound_Returns404()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new EntityNotFoundException("Experiment not found."));

        var result = await _compareModel.OnGetComparisonAsync(leftId, rightId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>(),
            "EntityNotFoundException must map to a JSON 404.");
    }

    [Test]
    public async Task OnGetComparisonAsync_ServiceThrowsConstraint_Returns409()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new EntityConflictException("Runs use different question sets."));

        var result = await _compareModel.OnGetComparisonAsync(leftId, rightId, CancellationToken.None);

        Assert.That(result, Is.AssignableTo<IStatusCodeActionResult>(),
            "Incompatible runs must map EntityConflictException to a JSON 409.");
        Assert.That(((IStatusCodeActionResult)result).StatusCode, Is.EqualTo(StatusCodes.Status409Conflict),
            "Incompatible runs must map EntityConflictException to a JSON 409.");
    }

    // ── Comparison compatibility requirements ─────────────────

    [Test]
    public async Task OnGetComparisonAsync_IncompatibleExperiments_ServiceReturnsNull_Returns404()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExperimentComparisonDto?)null);

        var result = await _compareModel.OnGetComparisonAsync(leftId, rightId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task OnGetComparisonAsync_BothIdsDistinct_CallsServiceExactlyOnce()
    {
        var leftId = Guid.NewGuid();
        var rightId = Guid.NewGuid();
        var dto = BuildComparison(leftId, rightId);
        _serviceMock.Setup(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        await _compareModel.OnGetComparisonAsync(leftId, rightId, CancellationToken.None);

        _serviceMock.Verify(s => s.CompareAsync(leftId, rightId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── PreparingIndex progress fields ────────────────────────

    [Test]
    public async Task OnGetResultAsync_PreparingIndexStatus_ShowsDocumentProgress()
    {
        var id = Guid.NewGuid();
        var dto = BuildResult(id, ExperimentStatus.PreparingIndex,
            indexedDocs: 2, affectedDocs: 4, completedQ: 0, totalQ: 3);
        _serviceMock.Setup(s => s.GetResultAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _resultsModel.OnGetResultAsync(id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (ExperimentResultDto)((JsonResult)result).Value!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(returned.Summary.IndexedDocumentCount, Is.EqualTo(2));
            Assert.That(returned.Summary.AffectedDocumentCount, Is.EqualTo(4));
            Assert.That(returned.Summary.CompletedQuestionCount, Is.Zero);
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private static ExperimentSummaryDto BuildSummary(
        ExperimentStatus status,
        int subjectId = 3,
        string subjectCode = "DB201",
        string questionSetKey = "DB201-VI-001|DB201-VI-002",
        int indexedDocs = 4,
        int affectedDocs = 4,
        int completedQ = 2,
        int totalQ = 2)
    {
        return new ExperimentSummaryDto
        {
            ExperimentId = Guid.NewGuid(),
            ExperimentName = $"Test run ({status})",
            SubjectId = subjectId,
            SubjectCode = subjectCode,
            Status = status,
            ChunkingStrategy = "FixedLength",
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = "test-embed",
            LlmModel = "test-llm",
            QuestionSetKey = questionSetKey,
            IndexedDocumentCount = indexedDocs,
            AffectedDocumentCount = affectedDocs,
            CompletedQuestionCount = completedQ,
            TotalQuestionCount = totalQ,
            AggregateScores = new RagasStyleScoresDto
            {
                Faithfulness = status == ExperimentStatus.Completed ? 0.85 : null,
                AnswerRelevancy = status == ExperimentStatus.Completed ? 0.80 : null,
                ContextPrecision = status == ExperimentStatus.Completed ? 0.75 : null,
                ContextRecall = status == ExperimentStatus.Completed ? 0.78 : null,
            },
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            CompletedAt = status == ExperimentStatus.Completed ? DateTime.UtcNow : null,
            FailureReason = status == ExperimentStatus.Failed ? "Evaluator timeout" : null,
        };
    }

    private static ExperimentResultDto BuildResult(
        Guid id,
        ExperimentStatus status,
        int indexedDocs = 4,
        int affectedDocs = 4,
        int completedQ = 2,
        int totalQ = 2)
    {
        return new ExperimentResultDto
        {
            Summary = BuildSummary(status, indexedDocs: indexedDocs, affectedDocs: affectedDocs,
                completedQ: completedQ, totalQ: totalQ) with
            { ExperimentId = id },
            Configuration = new ExperimentConfigurationSnapshotDto
            {
                SubjectId = 3,
                SubjectCode = "DB201",
                SubjectName = "Database Systems",
                ChunkingStrategy = "FixedLength",
                ChunkSize = 1000,
                ChunkOverlap = 200,
                EmbeddingModel = "test-embed",
                TopK = 10,
                SimilarityThreshold = 0.55,
                MaxContextChunks = 8,
                LlmModel = "test-llm",
                ChatTemperature = 0.2f,
                MaxHistoryMessages = 12,
                ChatPrompt = "You are EduChatAI.",
                ContextPrompt = "Use retrieved material.",
                NoContextRetrievedPrompt = "No material found.",
                CitationExtractionTemperature = 0,
                CitationExtractionPrompt = "Extract grounded citations.",
                JudgeModel = "test-judge",
                EvaluatorPromptVersion = "ragas-style-v1",
            },
            Questions = new List<ExperimentQuestionResultDto>
            {
                new()
                {
                    TestResponseId = Guid.NewGuid(),
                    TestQuestionId = 1,
                    ExternalId = "DB201-VI-001",
                    Question = "Test question?",
                    GroundTruth = "Test ground truth.",
                    Status = status == ExperimentStatus.Completed
                        ? ExperimentQuestionStatus.Completed
                        : ExperimentQuestionStatus.Pending,
                    GeneratedAnswer = status == ExperimentStatus.Completed ? "Generated answer." : null,
                    RetrievedContexts = status == ExperimentStatus.Completed
                        ? new List<string> { "Context 1" }.AsReadOnly()
                        : new List<string>().AsReadOnly(),
                    Scores = new RagasStyleScoresDto
                    {
                        Faithfulness = status == ExperimentStatus.Completed ? 0.85 : null,
                        AnswerRelevancy = status == ExperimentStatus.Completed ? 0.80 : null,
                        ContextPrecision = status == ExperimentStatus.Completed ? 0.75 : null,
                        ContextRecall = status == ExperimentStatus.Completed ? 0.78 : null,
                    },
                    PromptTokens = status == ExperimentStatus.Completed ? 400 : null,
                    CompletionTokens = status == ExperimentStatus.Completed ? 50 : null,
                    RetrievalTimeMs = status == ExperimentStatus.Completed ? 80L : null,
                    TimeToFirstTokenMs = status == ExperimentStatus.Completed ? 390L : null,
                    TotalResponseTimeMs = status == ExperimentStatus.Completed ? 1500L : null,
                },
            }.AsReadOnly(),
        };
    }

    private static ExperimentComparisonDto BuildComparison(Guid leftId, Guid rightId)
    {
        return new ExperimentComparisonDto
        {
            Left = BuildSummary(ExperimentStatus.Completed) with { ExperimentId = leftId },
            Right = BuildSummary(ExperimentStatus.Completed) with { ExperimentId = rightId },
            Metrics = new List<MetricComparisonDto>
            {
                new() { Metric = "Faithfulness",      LeftScore = 0.81, RightScore = 0.87, Delta = 0.06 },
                new() { Metric = "AnswerRelevancy",   LeftScore = 0.80, RightScore = 0.84, Delta = 0.04 },
                new() { Metric = "ContextPrecision",  LeftScore = 0.72, RightScore = 0.79, Delta = 0.07 },
                new() { Metric = "ContextRecall",     LeftScore = 0.76, RightScore = 0.82, Delta = 0.06 },
            }.AsReadOnly(),
            Questions = new List<QuestionScoreComparisonDto>
            {
                new()
                {
                    TestQuestionId = 1,
                    ExternalId = "DB201-VI-001",
                    Question = "Test question?",
                    LeftScores  = new RagasStyleScoresDto { Faithfulness = 0.81, AnswerRelevancy = 0.80, ContextPrecision = 0.72, ContextRecall = 0.76 },
                    RightScores = new RagasStyleScoresDto { Faithfulness = 0.87, AnswerRelevancy = 0.84, ContextPrecision = 0.79, ContextRecall = 0.82 },
                },
            }.AsReadOnly(),
        };
    }
}
