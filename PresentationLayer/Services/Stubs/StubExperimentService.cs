using Domain.Contracts;
using Domain.Contracts.DTOs;

namespace Presentation.Services.Stubs;

/// <summary>
/// STUB — Frontend preview only. Remove when the real IExperimentService is registered.
/// All data is hard-coded inline — no filesystem or database dependency.
/// </summary>
internal sealed class StubExperimentService : IExperimentService
{
    // ── Fixed GUIDs matching the fixture contract ─────────────────
    private static readonly Guid IdCompleted1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid IdCompleted2 = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid IdInProgress = Guid.Parse("20000000-0000-0000-0000-000000000003");

    // ── Not needed for C's pages ──────────────────────────────────

    public Task<ExperimentCreateOptionsDto?> GetCreateOptionsAsync(
        int subjectId, CancellationToken cancellationToken = default)
        => Task.FromResult<ExperimentCreateOptionsDto?>(null);

    public Task<ExperimentIndexPreflightDto?> PreflightAsync(
        ExperimentIndexPreflightRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult<ExperimentIndexPreflightDto?>(null);

    public Task<CreateExperimentResponse> CreateAsync(
        CreateExperimentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Stub: CreateAsync not supported for frontend preview.");

    // ── C's pages ────────────────────────────────────────────────

    public Task<IReadOnlyList<ExperimentSummaryDto>> GetSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExperimentSummaryDto> list = new List<ExperimentSummaryDto>
        {
            Summary1(), Summary2(), Summary3(),
        }.AsReadOnly();
        return Task.FromResult(list);
    }

    public Task<ExperimentResultDto?> GetResultAsync(
        Guid experimentId, CancellationToken cancellationToken = default)
    {
        ExperimentResultDto? result = experimentId == IdInProgress
            ? BuildResultPreparingIndex()
            : BuildResultCompleted(experimentId);
        return Task.FromResult<ExperimentResultDto?>(result);
    }

    public Task<ExperimentComparisonDto?> CompareAsync(
        Guid leftExperimentId, Guid rightExperimentId,
        CancellationToken cancellationToken = default)
    {
        var comparison = new ExperimentComparisonDto
        {
            Left  = Summary2() with { ExperimentId = leftExperimentId },
            Right = Summary1() with { ExperimentId = rightExperimentId },
            Metrics = new List<MetricComparisonDto>
            {
                new() { Metric = "Faithfulness",     LeftScore = 0.81, RightScore = 0.87, Delta = 0.06 },
                new() { Metric = "AnswerRelevancy",  LeftScore = 0.80, RightScore = 0.84, Delta = 0.04 },
                new() { Metric = "ContextPrecision", LeftScore = 0.72, RightScore = 0.79, Delta = 0.07 },
                new() { Metric = "ContextRecall",    LeftScore = 0.76, RightScore = 0.82, Delta = 0.06 },
            }.AsReadOnly(),
            Questions = new List<QuestionScoreComparisonDto>
            {
                new()
                {
                    TestQuestionId = 1,
                    ExternalId = "DB201-VI-001",
                    Question = "Chuẩn hóa cơ sở dữ liệu là gì?",
                    LeftScores  = Scores(0.84, 0.82, 0.75, 0.78),
                    RightScores = Scores(0.90, 0.88, 0.83, 0.86),
                },
                new()
                {
                    TestQuestionId = 2,
                    ExternalId = "DB201-VI-002",
                    Question = "Dạng chuẩn thứ nhất yêu cầu điều gì?",
                    LeftScores  = Scores(0.80, 0.80, 0.71, 0.76),
                    RightScores = Scores(0.86, 0.83, 0.78, 0.81),
                },
                new()
                {
                    TestQuestionId = 3,
                    ExternalId = "DB201-VI-003",
                    Question = "Chỉ mục B-tree giúp truy vấn như thế nào?",
                    LeftScores  = Scores(0.79, 0.78, 0.70, 0.74),
                    RightScores = Scores(0.85, 0.81, 0.76, 0.79),
                },
            }.AsReadOnly(),
        };
        return Task.FromResult<ExperimentComparisonDto?>(comparison);
    }

    // ── Summary builders ─────────────────────────────────────────

    private static ExperimentSummaryDto Summary1() => new()
    {
        ExperimentId = IdCompleted1,
        ExperimentName = "DB201 recursive Gemini smoke run",
        SubjectId = 3, SubjectCode = "DB201",
        Status = ExperimentStatus.Completed,
        ChunkingStrategy = "RecursiveSeparator",
        ChunkSize = 1200, ChunkOverlap = 180,
        EmbeddingModel = "nvidia/llama-nemotron-embed-vl-1b-v2:free",
        LlmModel = "gemini-3.5-flash",
        QuestionSetKey = "DB201-VI-001|DB201-VI-002|DB201-VI-003",
        IndexedDocumentCount = 4, AffectedDocumentCount = 4,
        CompletedQuestionCount = 3, TotalQuestionCount = 3,
        AggregateScores = Scores(0.87, 0.84, 0.79, 0.82),
        CreatedAt = new DateTime(2026, 7, 13, 3, 10, 0, DateTimeKind.Utc),
        CompletedAt = new DateTime(2026, 7, 13, 3, 22, 0, DateTimeKind.Utc),
        FailureReason = null,
    };

    private static ExperimentSummaryDto Summary2() => new()
    {
        ExperimentId = IdCompleted2,
        ExperimentName = "DB201 fixed Gemini smoke run",
        SubjectId = 3, SubjectCode = "DB201",
        Status = ExperimentStatus.Completed,
        ChunkingStrategy = "FixedLength",
        ChunkSize = 1000, ChunkOverlap = 200,
        EmbeddingModel = "nvidia/llama-nemotron-embed-vl-1b-v2:free",
        LlmModel = "gemini-3.5-flash",
        QuestionSetKey = "DB201-VI-001|DB201-VI-002|DB201-VI-003",
        IndexedDocumentCount = 4, AffectedDocumentCount = 4,
        CompletedQuestionCount = 3, TotalQuestionCount = 3,
        AggregateScores = Scores(0.81, 0.80, 0.72, 0.76),
        CreatedAt = new DateTime(2026, 7, 13, 2, 20, 0, DateTimeKind.Utc),
        CompletedAt = new DateTime(2026, 7, 13, 2, 31, 0, DateTimeKind.Utc),
        FailureReason = null,
    };

    private static ExperimentSummaryDto Summary3() => new()
    {
        ExperimentId = IdInProgress,
        ExperimentName = "DB201 sentence packing smoke run",
        SubjectId = 3, SubjectCode = "DB201",
        Status = ExperimentStatus.PreparingIndex,
        ChunkingStrategy = "SentenceParagraph",
        ChunkSize = 1000, ChunkOverlap = 120,
        EmbeddingModel = "gemini-embedding-2",
        LlmModel = "gemini-3.5-flash",
        QuestionSetKey = "DB201-VI-001|DB201-VI-002|DB201-VI-003",
        IndexedDocumentCount = 2, AffectedDocumentCount = 4,
        CompletedQuestionCount = 0, TotalQuestionCount = 3,
        AggregateScores = Scores(null, null, null, null),
        CreatedAt = new DateTime(2026, 7, 13, 4, 0, 0, DateTimeKind.Utc),
        CompletedAt = null,
        FailureReason = null,
    };

    // ── Result builders ──────────────────────────────────────────

    private static ExperimentResultDto BuildResultCompleted(Guid id) => new()
    {
        Summary = Summary1() with { ExperimentId = id },
        Configuration = Cfg(),
        Questions = new List<ExperimentQuestionResultDto>
        {
            Q(1, "DB201-VI-001", "Chuẩn hóa cơ sở dữ liệu là gì?",
              "Chuẩn hóa cơ sở dữ liệu tổ chức dữ liệu thành các bảng phù hợp để giảm dư thừa và hạn chế bất nhất.",
              "Chuẩn hóa tổ chức dữ liệu để giảm dư thừa và cải thiện tính nhất quán của cơ sở dữ liệu.",
              new[] { "Database normalization reduces redundancy and improves data consistency." },
              Scores(0.90, 0.88, 0.83, 0.86), 412, 48, 74, 386, 1420),
            Q(2, "DB201-VI-002", "Dạng chuẩn thứ nhất yêu cầu điều gì?",
              "Dạng chuẩn thứ nhất yêu cầu mỗi ô chứa một giá trị nguyên tử và không có nhóm dữ liệu lặp.",
              "Dạng chuẩn thứ nhất yêu cầu các giá trị trong mỗi ô phải nguyên tử và loại bỏ nhóm lặp.",
              new[] { "First Normal Form requires atomic column values and no repeating groups." },
              Scores(0.86, 0.83, 0.78, 0.81), 438, 51, 81, 402, 1512),
            Q(3, "DB201-VI-003", "Chỉ mục B-tree giúp truy vấn như thế nào?",
              "Chỉ mục B-tree giữ khóa theo cấu trúc cân bằng, giúp tìm kiếm, sắp xếp và truy vấn theo khoảng hiệu quả.",
              "B-tree duy trì các khóa trong cây cân bằng để tăng tốc tìm kiếm và truy vấn theo khoảng.",
              new[] { "B-tree indexes keep keys in a balanced structure and support efficient lookups and range scans." },
              Scores(0.85, 0.81, 0.76, 0.79), 455, 55, 88, 417, 1588),
        }.AsReadOnly(),
    };

    private static ExperimentResultDto BuildResultPreparingIndex() => new()
    {
        Summary = Summary3(),
        Configuration = Cfg(),
        Questions = new List<ExperimentQuestionResultDto>
        {
            Q(1, "DB201-VI-001", "Chuẩn hóa cơ sở dữ liệu là gì?",
              "Chuẩn hóa cơ sở dữ liệu tổ chức dữ liệu thành các bảng phù hợp để giảm dư thừa và hạn chế bất nhất.",
              null, Array.Empty<string>(),
              Scores(null, null, null, null), null, null, null, null, null),
            Q(2, "DB201-VI-002", "Dạng chuẩn thứ nhất yêu cầu điều gì?",
              "Dạng chuẩn thứ nhất yêu cầu mỗi ô chứa một giá trị nguyên tử và không có nhóm dữ liệu lặp.",
              null, Array.Empty<string>(),
              Scores(null, null, null, null), null, null, null, null, null),
            Q(3, "DB201-VI-003", "Chỉ mục B-tree giúp truy vấn như thế nào?",
              "Chỉ mục B-tree giữ khóa theo cấu trúc cân bằng, giúp tìm kiếm, sắp xếp và truy vấn theo khoảng hiệu quả.",
              null, Array.Empty<string>(),
              Scores(null, null, null, null), null, null, null, null, null),
        }.AsReadOnly(),
    };

    // ── Shared helpers ───────────────────────────────────────────

    private static ExperimentConfigurationSnapshotDto Cfg() => new()
    {
        SubjectId = 3, SubjectCode = "DB201", SubjectName = "Database Systems",
        ChunkingStrategy = "RecursiveSeparator",
        ChunkSize = 1200, ChunkOverlap = 180,
        EmbeddingModel = "nvidia/llama-nemotron-embed-vl-1b-v2:free",
        TopK = 12, SimilarityThreshold = 0.55, MaxContextChunks = 8,
        LlmModel = "gemini-3.5-flash",
        ChatTemperature = 0.2f, MaxHistoryMessages = 12,
        ChatPrompt = "You are EduChatAI, an educational assistant.",
        ContextPrompt = "Use retrieved course material.",
        NoContextRetrievedPrompt = "State that no course material was found.",
        JudgeModel = "gemini-3.5-flash",
        EvaluatorPromptVersion = "ragas-style-v1",
    };

    private static RagasStyleScoresDto Scores(
        double? f, double? ar, double? cp, double? cr)
        => new() { Faithfulness = f, AnswerRelevancy = ar, ContextPrecision = cp, ContextRecall = cr };

    private static ExperimentQuestionResultDto Q(
        int questionId, string externalId, string question, string groundTruth,
        string? generatedAnswer, string[] contexts,
        RagasStyleScoresDto scores,
        int? promptTokens, int? completionTokens,
        long? retrievalMs, long? ttftMs, long? totalMs)
    {
        var isComplete = generatedAnswer != null;
        return new ExperimentQuestionResultDto
        {
            TestResponseId = Guid.NewGuid(),
            TestQuestionId = questionId,
            ExternalId = externalId,
            Question = question,
            GroundTruth = groundTruth,
            Status = isComplete ? ExperimentQuestionStatus.Completed : ExperimentQuestionStatus.Pending,
            GeneratedAnswer = generatedAnswer,
            RetrievedContexts = contexts.ToList().AsReadOnly(),
            Scores = scores,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            RetrievalTimeMs = retrievalMs,
            TimeToFirstTokenMs = ttftMs,
            TotalResponseTimeMs = totalMs,
        };
    }
}
