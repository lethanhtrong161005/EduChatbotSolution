# EduChatAI Three-Day Delivery Contracts

Last verified: 2026-07-13  
Status: Frozen for implementation after explicit Phase 2 approval

Repository code remains authoritative for implemented behavior. This document freezes the planned cross-owner contracts; proposed entity and migration decisions are not implemented by this documentation task.

## Serialization And HTTP Conventions

- Razor Page JSON uses the existing ASP.NET Core web defaults: camel-case property names, ISO 8601 timestamps, and numeric enum values.
- Requests that mutate data include the antiforgery header `RequestVerificationToken`.
- Chat mutations also include `CallerConnectionId` and `ChatConnectionId` when the existing page has those SignalR connections.
- Admin pages use `[Authorize(Roles = "Admin")]`; chat uses `[Authorize]` plus an owned-session check.
- `400` means invalid request or invalid state transition, `401` unauthenticated, `403` authenticated but unauthorized, `404` absent or not owned, and `409` a concurrent state conflict.
- Cancellation propagates. Unexpected exceptions are not converted into authorization failures.
- All public domain service methods use the parameter name `cancellationToken`.

## Contract Ownership

| Contract producer | Consumer | Contract code owner | UI owner |
|---|---|---|---|
| Subject AI configuration and reindex services | AI configuration and experiment-create pages | Owner | B |
| Admin report service | Report dashboard | Owner | C |
| Experiment service and runner | Experiment create, results, and comparison pages | Owner | B for create; C for results/comparison |
| Vietnamese test dataset | Experiment importer and runner | Owner for DTO/import; B for JSON/tests | B |
| Chat variant persistence foundation | Chat PageModel, coordinator, SignalR, and client | Owner | A |

The owner creates the DTOs and service interfaces before teammate branches begin. Teammates must not rename properties, handlers, routes, or enum values. If an implemented signature cannot match this document, work stops and the conflict is reported before any contract edit.

## 1. Subject AI Configuration

### Domain DTOs

File: `Domain/Contracts/DTOs/AiConfigurationAdminDtos.cs`  
Namespace: `Domain.Contracts.DTOs`

```csharp
public record AiStringSettingDto
{
    public required string GlobalDefault { get; init; }
    public string? StoredOverride { get; init; }
    public required string EffectiveValue { get; init; }
}

public record AiIntSettingDto
{
    public required int GlobalDefault { get; init; }
    public int? StoredOverride { get; init; }
    public required int EffectiveValue { get; init; }
}

public record AiDoubleSettingDto
{
    public required double GlobalDefault { get; init; }
    public double? StoredOverride { get; init; }
    public required double EffectiveValue { get; init; }
}

public record AiFloatSettingDto
{
    public required float GlobalDefault { get; init; }
    public float? StoredOverride { get; init; }
    public required float EffectiveValue { get; init; }
}

public record AiIndexingConfigurationDto
{
    public required AiStringSettingDto ChunkingStrategy { get; init; }
    public required AiIntSettingDto ChunkSize { get; init; }
    public required AiIntSettingDto ChunkOverlap { get; init; }
    public required AiStringSettingDto EmbeddingModel { get; init; }
}

public record AiRetrievalConfigurationDto
{
    public required AiIntSettingDto TopK { get; init; }
    public required AiDoubleSettingDto SimilarityThreshold { get; init; }
    public required AiIntSettingDto MaxContextChunks { get; init; }
}

public record AiGenerationConfigurationDto
{
    public required AiStringSettingDto LlmModel { get; init; }
    public required AiFloatSettingDto ChatTemperature { get; init; }
    public required AiIntSettingDto MaxHistoryMessages { get; init; }
    public required AiFloatSettingDto TitleTemperature { get; init; }
    public required AiFloatSettingDto CitationExtractionTemperature { get; init; }
}

public record AiPromptConfigurationDto
{
    public required AiStringSettingDto ChatPrompt { get; init; }
    public required AiStringSettingDto ContextPrompt { get; init; }
    public required AiStringSettingDto NoContextRetrievedPrompt { get; init; }
    public required AiStringSettingDto TitlePrompt { get; init; }
    public required AiStringSettingDto CitationExtractionPrompt { get; init; }
}

public record SubjectAiConfigurationDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required AiIndexingConfigurationDto Indexing { get; init; }
    public required AiRetrievalConfigurationDto Retrieval { get; init; }
    public required AiGenerationConfigurationDto Generation { get; init; }
    public required AiPromptConfigurationDto Prompts { get; init; }
}

public record SaveSubjectAiConfigurationRequest
{
    public string? ChunkingStrategy { get; init; }
    public int? ChunkSize { get; init; }
    public int? ChunkOverlap { get; init; }
    public string? EmbeddingModel { get; init; }
    public int? TopK { get; init; }
    public double? SimilarityThreshold { get; init; }
    public int? MaxContextChunks { get; init; }
    public string? LlmModel { get; init; }
    public float? ChatTemperature { get; init; }
    public int? MaxHistoryMessages { get; init; }
    public string? ChatPrompt { get; init; }
    public string? ContextPrompt { get; init; }
    public string? NoContextRetrievedPrompt { get; init; }
    public float? TitleTemperature { get; init; }
    public string? TitlePrompt { get; init; }
    public float? CitationExtractionTemperature { get; init; }
    public string? CitationExtractionPrompt { get; init; }
}

public record AiOptionDto
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

public record AiConfigurationOptionsDto
{
    public required IReadOnlyList<AiOptionDto> ChunkingStrategies { get; init; }
    public required IReadOnlyList<AiOptionDto> EmbeddingModels { get; init; }
    public required IReadOnlyList<AiOptionDto> ChatModels { get; init; }
    public required IReadOnlyList<AiOptionDto> JudgeModels { get; init; }
}

public record AiConfigurationSubjectOptionDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
}

public record SubjectReindexResponseDto
{
    public required int SubjectId { get; init; }
    public required int QueuedDocumentCount { get; init; }
    public required DateTime QueuedAt { get; init; }
}
```

Every property in `SaveSubjectAiConfigurationRequest` is sent by the client. JSON null means “remove the subject override and inherit the global value”; it does not mean “leave unchanged.” Empty or whitespace strings are invalid for model names, strategies, and prompts. `ChunkSize` is `100..8000`; `ChunkOverlap` is `0..ChunkSize - 1`; `TopK` and context/history limits are positive; similarity is `[0,1]`; temperatures are `[0,2]`.

Allowed strategy values are exactly `FixedLength`, `RecursiveSeparator`, and `SentenceParagraph`. Default global chunk size and overlap remain `1000` and `200`.

### Service

File: `Domain/Contracts/IAiConfigurationAdminService.cs`  
Namespace: `Domain.Contracts`

```csharp
public interface IAiConfigurationAdminService
{
    Task<IReadOnlyList<AiConfigurationSubjectOptionDto>> GetSubjectsAsync(
        CancellationToken cancellationToken = default);

    Task<AiConfigurationOptionsDto> GetOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<SubjectAiConfigurationDto?> GetSubjectConfigurationAsync(
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<SubjectAiConfigurationDto> SaveSubjectConfigurationAsync(
        int subjectId,
        SaveSubjectAiConfigurationRequest request,
        CancellationToken cancellationToken = default);

    Task<SubjectReindexResponseDto> ReindexSubjectAsync(
        int subjectId,
        CancellationToken cancellationToken = default);
}
```

### Razor Page

Files owned by B:

- `PresentationLayer/Pages/Admin/AiConfiguration.cshtml`
- `PresentationLayer/Pages/Admin/AiConfiguration.cshtml.cs`
- `PresentationLayer/wwwroot/js/admin/admin-ai-configuration.js`
- `PresentationLayer/wwwroot/css/admin-ai-configuration.css`

Namespace and PageModel: `Presentation.Pages.Admin.AiConfigurationModel`.

| Method | Route | Handler signature | Response |
|---|---|---|---|
| GET | `/admin/ai-configuration` | `OnGetAsync(CancellationToken cxlTkn)` | Razor page |
| GET | `/admin/ai-configuration?handler=Subjects` | `OnGetSubjectsAsync(CancellationToken cxlTkn)` | `200 IReadOnlyList<AiConfigurationSubjectOptionDto>` |
| GET | `/admin/ai-configuration?handler=Options` | `OnGetOptionsAsync(CancellationToken cxlTkn)` | `200 AiConfigurationOptionsDto` |
| GET | `/admin/ai-configuration?handler=Configuration&subjectId={int}` | `OnGetConfigurationAsync([FromQuery] int subjectId, CancellationToken cxlTkn)` | `200 SubjectAiConfigurationDto`, `404` |
| PUT | `/admin/ai-configuration?handler=Configuration&subjectId={int}` | `OnPutConfigurationAsync([FromQuery] int subjectId, [FromBody] SaveSubjectAiConfigurationRequest request, CancellationToken cxlTkn)` | `200 SubjectAiConfigurationDto`, `400`, `404` |
| POST | `/admin/ai-configuration?handler=Reindex&subjectId={int}` | `OnPostReindexAsync([FromQuery] int subjectId, CancellationToken cxlTkn)` | `202 SubjectReindexResponseDto`, `404`, `409` |

Fixtures:

- [subject-ai-configuration.json](fixtures/subject-ai-configuration.json)
- [ai-configuration-options.json](fixtures/ai-configuration-options.json)
- [subject-reindex-response.json](fixtures/subject-reindex-response.json)

## 2. Admin Report Dashboard

### Domain DTOs

File: `Domain/Contracts/DTOs/AdminReportDtos.cs`  
Namespace: `Domain.Contracts.DTOs`

```csharp
public enum ReportRange
{
    Last7Days = 0,
    Last30Days = 1,
    AllTime = 2,
}

public enum ReportRoleFilter
{
    All = 0,
    Student = 1,
    Lecturer = 2,
    Admin = 3,
}

public record TokenMeasurementDto
{
    public required long? MeasuredPromptTokens { get; init; }
    public required long? MeasuredCompletionTokens { get; init; }
    public required long? MeasuredTotalTokens { get; init; }
    public required int MeasuredMessageCount { get; init; }
    public required int EligibleMessageCount { get; init; }
    public required double CoveragePercent { get; init; }
}

public record AdminReportKpiDto
{
    public required long UniqueActiveUserCount { get; init; }
    public required long ActiveSessionCount { get; init; }
    public required long CompletedAssistantGenerationCount { get; init; }
    public required long TerminalGenerationCount { get; init; }
    public required double? GenerationSuccessRatePercent { get; init; }
    public required double? CitationCoveragePercent { get; init; }
    public required long? P95TotalResponseTimeMs { get; init; }
    public required TokenMeasurementDto TokenMeasurement { get; init; }
}

public record ReportSubjectOptionDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
}

public record DailyAssistantGenerationDto
{
    public required DateOnly Date { get; init; }
    public required int CompletedAssistantGenerationCount { get; init; }
}

public record DailyTokenUsageDto
{
    public required DateOnly Date { get; init; }
    public required TokenMeasurementDto TokenMeasurement { get; init; }
}

public record DailyResponseLatencyDto
{
    public required DateOnly Date { get; init; }
    public required long? P95TotalResponseTimeMs { get; init; }
}

public record SubjectUsageDto
{
    public required int? SubjectId { get; init; }
    public required string? SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required int UniqueActiveUserCount { get; init; }
    public required int ActiveSessionCount { get; init; }
    public required int CompletedAssistantGenerationCount { get; init; }
    public required TokenMeasurementDto TokenMeasurement { get; init; }
}

public record SubjectHealthDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required int CompletedAssistantGenerationCount { get; init; }
    public required double NoContextRatePercent { get; init; }
    public required double GenerationFailureRatePercent { get; init; }
    public required long? P95TotalResponseTimeMs { get; init; }
}

public record IndexingStatusSummaryDto
{
    public required int IndexedDocumentCount { get; init; }
    public required int ProcessingDocumentCount { get; init; }
    public required int FailedDocumentCount { get; init; }
}

public record HotDocumentDto
{
    public required Guid DocumentId { get; init; }
    public required string DocumentTitle { get; init; }
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required int CitedAnswerCount { get; init; }
}

public record AdminReportDashboardDto
{
    public required ReportRange Range { get; init; }
    public required ReportRoleFilter Role { get; init; }
    public required int? TrendSubjectId { get; init; }
    public required DateTime? FromUtc { get; init; }
    public required DateTime ToUtc { get; init; }
    public required IReadOnlyList<ReportSubjectOptionDto> TrendSubjectOptions { get; init; }
    public required AdminReportKpiDto Kpis { get; init; }
    public required IReadOnlyList<DailyAssistantGenerationDto> AssistantGenerations { get; init; }
    public required IReadOnlyList<DailyTokenUsageDto> TokenUsage { get; init; }
    public required IReadOnlyList<DailyResponseLatencyDto> ResponseLatency { get; init; }
    public required IReadOnlyList<SubjectUsageDto> SubjectUsage { get; init; }
    public required IReadOnlyList<SubjectHealthDto> SubjectsNeedingAttention { get; init; }
    public required IndexingStatusSummaryDto IndexingStatus { get; init; }
    public required IReadOnlyList<HotDocumentDto> HotDocuments { get; init; }
}
```

The range and role filters apply to every chat-derived KPI, trend, subject row, health row, and hot-document row. `TrendSubjectId` applies only to `AssistantGenerations`, `TokenUsage`, and `ResponseLatency`; null means all subjects. `SubjectUsage` always compares all subjects. The indexing-status snapshot ignores range, role, and trend-subject filters.

An active user is a distinct session owner with at least one user message in the selected range. An active session is a distinct session with at least one user message in the range. Generation counts include every completed assistant variant, selected or not, because every variant is a real generation. The success-rate denominator is terminal assistant rows (`Completed` plus `Failed`); the rate is null when there are no terminal rows. Citation coverage is completed subject-bound assistant generations with at least one persisted citation divided by all completed subject-bound assistant generations; it is null when the denominator is zero.

A message is token-measured only when both provider prompt and completion counts are present. If eligible count is zero, all three measured-token values are zero and coverage is `100`. If eligible messages exist but none are measured, all three values are null and coverage is `0`. Partial coverage returns known prompt, completion, and total sums with measured/eligible counts. P95 uses the nearest-rank method and is null for an empty set.

Daily buckets use the organization timezone `SE Asia Standard Time` (`Asia/Bangkok`); `FromUtc` and `ToUtc` expose the corresponding UTC boundaries. Seven- and thirty-day responses include every local calendar date, including zero-value dates. All-time starts on the earliest matching chat event. The subjectless-session bucket has null subject ID/code and name `All subjects`.

`SubjectsNeedingAttention` contains only real subjects with at least ten completed assistant generations in the selected range. It is ordered by descending no-context rate, then generation-failure rate. A no-context generation has `ContextChunkCount == 0`. `ProcessingDocumentCount` combines `Received`, `Parsing`, `Parsed`, `Chunking`, `Chunked`, and `Embedding` documents. Hot documents count distinct `(ChatMessageId, DocumentId)` pairs so multiple chunks or occurrences from one answer count once.

### Service and Razor Page

File: `Domain/Contracts/IAdminReportService.cs`  
Namespace: `Domain.Contracts`

```csharp
public interface IAdminReportService
{
    Task<AdminReportDashboardDto> GetDashboardAsync(
        ReportRange range,
        ReportRoleFilter role,
        int? trendSubjectId,
        CancellationToken cancellationToken = default);
}
```

Files owned by C:

- `PresentationLayer/Pages/Admin/Reports.cshtml`
- `PresentationLayer/Pages/Admin/Reports.cshtml.cs`
- `PresentationLayer/wwwroot/js/admin/admin-reports.js`
- `PresentationLayer/wwwroot/css/admin-reports.css`

Namespace and PageModel: `Presentation.Pages.Admin.ReportsModel`.

| Method | Route | Handler signature | Response |
|---|---|---|---|
| GET | `/admin/reports` | `OnGetAsync(CancellationToken cxlTkn)` | Razor page |
| GET | `/admin/reports?handler=Dashboard&range={Last7Days\|Last30Days\|AllTime}&role={All\|Student\|Lecturer\|Admin}&trendSubjectId={int?}` | `OnGetDashboardAsync([FromQuery] ReportRange range, [FromQuery] ReportRoleFilter role, [FromQuery] int? trendSubjectId, CancellationToken cxlTkn)` | `200 AdminReportDashboardDto`, `400`, `404` |

The page defaults to `Last7Days`, `All`, and a null trend subject. It renders three daily charts, a horizontal subject chart with a metric selector, a compact all-subject table, a subject-health table, a hot-document table, and one indexing-status donut. Changing the trend subject must not filter the all-subject chart or table. Fixture: [admin-report-dashboard.json](fixtures/admin-report-dashboard.json).

## 3. Experiments

### Owner index-compatibility foundation

The owner adds this persisted state before B or C connects live handlers:

```csharp
public enum SubjectIndexAvailability
{
    Ready = 0,
    Reindexing = 1,
    Failed = 2,
}
```

`Domain.Entities.Subject` adds required `SubjectIndexAvailability IndexAvailability` with database default `Ready`. `Domain.Entities.Document` adds nullable `IndexedChunkingStrategy`, `IndexedChunkSize`, `IndexedChunkOverlap`, and `IndexedEmbeddingModel`. The indexer writes all four document values only after the complete chunk-and-embed path reaches `Indexed`. A subject-wide reindex sets `IndexAvailability = Reindexing` before replacing chunks, `Ready` only after every affected document succeeds, and `Failed` on any terminal failure.

The migration leaves the four new document values null for existing rows because chunk size and overlap cannot be reconstructed reliably. Their first compatibility check therefore requires reindexing. Production chat checks `Subject.IndexAvailability` before retrieval and returns a temporary-unavailable result unless it is `Ready`. Manual Admin reindex and experiment reindex use the same per-subject exclusive lock; a successful manual reindex recovers `Failed` to `Ready`.

### Domain DTOs

File: `Domain/Contracts/DTOs/ExperimentDtos.cs`  
Namespace: `Domain.Contracts.DTOs`

```csharp
public enum ExperimentStatus
{
    Queued = 0,
    PreparingIndex = 1,
    Running = 2,
    Evaluating = 3,
    Completed = 4,
    Failed = 5,
}

public enum ExperimentQuestionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}

public record CreateExperimentRequest
{
    public required string ExperimentName { get; init; }
    public required int SubjectId { get; init; }
    public required IReadOnlyList<int> TestQuestionIds { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
    public required int TopK { get; init; }
    public required double SimilarityThreshold { get; init; }
    public required int MaxContextChunks { get; init; }
    public required string LlmModel { get; init; }
    public required float ChatTemperature { get; init; }
    public required string JudgeModel { get; init; }
    public string? Notes { get; init; }
}

public record ExperimentIndexPreflightRequest
{
    public required int SubjectId { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
}

public record ExperimentIndexPreflightDto
{
    public required bool IsCompatible { get; init; }
    public required bool RequiresReindex { get; init; }
    public required int AffectedDocumentCount { get; init; }
    public string? BlockingReason { get; init; }
}

public record CreateExperimentResponse
{
    public required Guid ExperimentId { get; init; }
    public required ExperimentStatus Status { get; init; }
    public required DateTime QueuedAt { get; init; }
    public required int QuestionCount { get; init; }
    public required bool RequiresReindex { get; init; }
    public required int AffectedDocumentCount { get; init; }
}

public record TestQuestionOptionDto
{
    public required int TestQuestionId { get; init; }
    public required string ExternalId { get; init; }
    public required string Question { get; init; }
}

public record ExperimentCreateOptionsDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required SubjectAiConfigurationDto CurrentConfiguration { get; init; }
    public required AiConfigurationOptionsDto AiOptions { get; init; }
    public required IReadOnlyList<TestQuestionOptionDto> TestQuestions { get; init; }
}

public record ExperimentConfigurationSnapshotDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
    public required int TopK { get; init; }
    public required double SimilarityThreshold { get; init; }
    public required int MaxContextChunks { get; init; }
    public required string LlmModel { get; init; }
    public required float ChatTemperature { get; init; }
    public required int MaxHistoryMessages { get; init; }
    public required string ChatPrompt { get; init; }
    public required string ContextPrompt { get; init; }
    public required string NoContextRetrievedPrompt { get; init; }
    public required string JudgeModel { get; init; }
    public required string EvaluatorPromptVersion { get; init; }
}

public record RagasStyleScoresDto
{
    public required double? Faithfulness { get; init; }
    public required double? AnswerRelevancy { get; init; }
    public required double? ContextPrecision { get; init; }
    public required double? ContextRecall { get; init; }
}

public record ExperimentSummaryDto
{
    public required Guid ExperimentId { get; init; }
    public required string ExperimentName { get; init; }
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required ExperimentStatus Status { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
    public required string LlmModel { get; init; }
    public required string QuestionSetKey { get; init; }
    public required int IndexedDocumentCount { get; init; }
    public required int AffectedDocumentCount { get; init; }
    public required int CompletedQuestionCount { get; init; }
    public required int TotalQuestionCount { get; init; }
    public required RagasStyleScoresDto AggregateScores { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public string? FailureReason { get; init; }
}

public record ExperimentQuestionResultDto
{
    public required Guid TestResponseId { get; init; }
    public required int TestQuestionId { get; init; }
    public required string ExternalId { get; init; }
    public required string Question { get; init; }
    public required string GroundTruth { get; init; }
    public required ExperimentQuestionStatus Status { get; init; }
    public required string? GeneratedAnswer { get; init; }
    public required IReadOnlyList<string> RetrievedContexts { get; init; }
    public required RagasStyleScoresDto Scores { get; init; }
    public string? Explanation { get; init; }
    public string? FailureReason { get; init; }
    public required int? PromptTokens { get; init; }
    public required int? CompletionTokens { get; init; }
    public required long? RetrievalTimeMs { get; init; }
    public required long? TimeToFirstTokenMs { get; init; }
    public required long? TotalResponseTimeMs { get; init; }
}

public record ExperimentResultDto
{
    public required ExperimentSummaryDto Summary { get; init; }
    public required ExperimentConfigurationSnapshotDto Configuration { get; init; }
    public required IReadOnlyList<ExperimentQuestionResultDto> Questions { get; init; }
}

public record MetricComparisonDto
{
    public required string Metric { get; init; }
    public required double? LeftScore { get; init; }
    public required double? RightScore { get; init; }
    public required double? Delta { get; init; }
}

public record QuestionScoreComparisonDto
{
    public required int TestQuestionId { get; init; }
    public required string ExternalId { get; init; }
    public required string Question { get; init; }
    public required RagasStyleScoresDto LeftScores { get; init; }
    public required RagasStyleScoresDto RightScores { get; init; }
}

public record ExperimentComparisonDto
{
    public required ExperimentSummaryDto Left { get; init; }
    public required ExperimentSummaryDto Right { get; init; }
    public required IReadOnlyList<MetricComparisonDto> Metrics { get; init; }
    public required IReadOnlyList<QuestionScoreComparisonDto> Questions { get; init; }
}
```

All selected test questions must belong to `DB201`, be Vietnamese, and have Vietnamese ground truth. `TestQuestionIds` has 1 to 50 distinct IDs. `QuestionSetKey` is the selected external IDs sorted ordinally and joined by `|`; it lets the summary table test comparison compatibility without loading question details. Scores are null until evaluated or after a failed question; completed evaluation scores are finite and in `[0,1]`. Comparison requires exactly two completed runs with the same subject and identical `QuestionSetKey`.

Preflight compares every subject document with the requested `ChunkingStrategy`, `ChunkSize`, `ChunkOverlap`, and `EmbeddingModel`. It is compatible only when the subject has at least one document and every document is `Indexed` with all four successful-index values matching. Null legacy values are incompatible and force a safe reindex. Retrieval, generation, prompt, and judge settings never require reindexing. A subject with no documents, or one already locked by reindex/experiment work, returns a non-null `BlockingReason`, `RequiresReindex = false`, and cannot be submitted.

Creation repeats preflight server-side. A compatible run moves from `Queued` to `Running`; an incompatible run moves through `PreparingIndex`, keeps `IndexedDocumentCount` current, and starts questions only after all affected documents are `Indexed`. The results route opens immediately and polls the result contract through `Queued`, `PreparingIndex`, `Running`, `Evaluating`, `Completed`, or `Failed`. The UI shows progress counts and never promises an estimated duration.

### Evaluator contract

File: `Domain/Contracts/IRagasStyleEvaluator.cs`  
Namespace: `Domain.Contracts`

```csharp
public interface IRagasStyleEvaluator
{
    Task<RagasStyleEvaluationResult> EvaluateAsync(
        RagasStyleEvaluationRequest request,
        CancellationToken cancellationToken = default);
}

public record RagasStyleEvaluationRequest
{
    public required string Question { get; init; }
    public required string GroundTruth { get; init; }
    public required string GeneratedAnswer { get; init; }
    public required IReadOnlyList<string> RetrievedContexts { get; init; }
    public required string JudgeModel { get; init; }
}

public record RagasStyleEvaluationResult
{
    public required double Faithfulness { get; init; }
    public required double AnswerRelevancy { get; init; }
    public required double ContextPrecision { get; init; }
    public required double ContextRecall { get; init; }
    public string? Explanation { get; init; }
}
```

### Service

File: `Domain/Contracts/IExperimentService.cs`  
Namespace: `Domain.Contracts`

```csharp
public interface IExperimentService
{
    Task<ExperimentCreateOptionsDto?> GetCreateOptionsAsync(
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<ExperimentIndexPreflightDto?> PreflightAsync(
        ExperimentIndexPreflightRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateExperimentResponse> CreateAsync(
        CreateExperimentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExperimentSummaryDto>> GetSummariesAsync(
        CancellationToken cancellationToken = default);

    Task<ExperimentResultDto?> GetResultAsync(
        Guid experimentId,
        CancellationToken cancellationToken = default);

    Task<ExperimentComparisonDto?> CompareAsync(
        Guid leftExperimentId,
        Guid rightExperimentId,
        CancellationToken cancellationToken = default);
}
```

### Razor Pages

B owns experiment creation:

- `PresentationLayer/Pages/Admin/Experiments/Create.cshtml`
- `PresentationLayer/Pages/Admin/Experiments/Create.cshtml.cs`
- `PresentationLayer/wwwroot/js/admin/admin-experiment-create.js`
- `PresentationLayer/wwwroot/css/admin-experiment-create.css`

Namespace/PageModel: `Presentation.Pages.Admin.Experiments.CreateModel`.

| Method | Route | Handler | Response |
|---|---|---|---|
| GET | `/admin/experiments/create` | `OnGetAsync(CancellationToken cxlTkn)` | Razor page |
| GET | `/admin/experiments/create?handler=Options&subjectId={int}` | `OnGetOptionsAsync([FromQuery] int subjectId, CancellationToken cxlTkn)` | `200 ExperimentCreateOptionsDto`, `404` |
| POST | `/admin/experiments/create?handler=Preflight` | `OnPostPreflightAsync([FromBody] ExperimentIndexPreflightRequest request, CancellationToken cxlTkn)` | `200 ExperimentIndexPreflightDto`, `400`, `404`, `409` |
| POST | `/admin/experiments/create?handler=Experiment` | `OnPostExperimentAsync([FromBody] CreateExperimentRequest request, CancellationToken cxlTkn)` | `202 CreateExperimentResponse`, `400`, `404`, `409` |

C owns results and comparison:

- `PresentationLayer/Pages/Admin/Experiments/Results.cshtml`
- `PresentationLayer/Pages/Admin/Experiments/Results.cshtml.cs`
- `PresentationLayer/Pages/Admin/Experiments/Compare.cshtml`
- `PresentationLayer/Pages/Admin/Experiments/Compare.cshtml.cs`
- `PresentationLayer/wwwroot/js/admin/admin-experiment-results.js`
- `PresentationLayer/wwwroot/js/admin/admin-experiment-compare.js`
- `PresentationLayer/wwwroot/css/admin-experiment-results.css`

Namespaces/PageModels: `Presentation.Pages.Admin.Experiments.ResultsModel` and `Presentation.Pages.Admin.Experiments.CompareModel`.

| Method | Route | Handler | Response |
|---|---|---|---|
| GET | `/admin/experiments/results?id={guid}` | `ResultsModel.OnGetAsync([FromQuery] Guid? id, CancellationToken cxlTkn)` | Razor page |
| GET | `/admin/experiments/results?handler=Summaries` | `ResultsModel.OnGetSummariesAsync(CancellationToken cxlTkn)` | `200 IReadOnlyList<ExperimentSummaryDto>` |
| GET | `/admin/experiments/results?handler=Result&id={guid}` | `ResultsModel.OnGetResultAsync([FromQuery] Guid id, CancellationToken cxlTkn)` | `200 ExperimentResultDto`, `404` |
| GET | `/admin/experiments/compare?leftId={guid}&rightId={guid}` | `CompareModel.OnGetAsync([FromQuery] Guid? leftId, [FromQuery] Guid? rightId, CancellationToken cxlTkn)` | Razor page |
| GET | `/admin/experiments/compare?handler=Comparison&leftId={guid}&rightId={guid}` | `CompareModel.OnGetComparisonAsync([FromQuery] Guid leftId, [FromQuery] Guid rightId, CancellationToken cxlTkn)` | `200 ExperimentComparisonDto`, `400`, `404`, `409` |

The results page always lists every run in a compact table using summary configuration and progress fields. Only completed runs are selectable. The Compare action is enabled only when exactly two runs with the same subject and identical question IDs are selected; the comparison service remains pairwise and no run-history limit is introduced.

Fixtures:

- [experiment-create-options.json](fixtures/experiment-create-options.json)
- [experiment-index-preflight-request.json](fixtures/experiment-index-preflight-request.json)
- [experiment-index-preflight.json](fixtures/experiment-index-preflight.json)
- [experiment-create-request.json](fixtures/experiment-create-request.json)
- [experiment-create-response.json](fixtures/experiment-create-response.json)
- [experiment-summaries.json](fixtures/experiment-summaries.json)
- [experiment-result.json](fixtures/experiment-result.json)
- [experiment-result-preparing-index.json](fixtures/experiment-result-preparing-index.json)
- [experiment-comparison.json](fixtures/experiment-comparison.json)

### Vietnamese dataset contract

File: `Domain/Contracts/DTOs/TestDatasetDtos.cs`  
Namespace: `Domain.Contracts.DTOs`

B creates `BusinessLayer/Services/AI/Experiments/Data/db201-vi-50.json` and `UnitTests/VietnameseExperimentDatasetTests.cs`; the owner alone changes `Business.csproj` and import/seed wiring.

```csharp
public record TestDatasetDto
{
    public required string DatasetKey { get; init; }
    public required string Language { get; init; }
    public required string SubjectCode { get; init; }
    public required IReadOnlyList<TestDatasetQuestionDto> Questions { get; init; }
}

public record TestDatasetQuestionDto
{
    public required string ExternalId { get; init; }
    public required string Question { get; init; }
    public required string GroundTruth { get; init; }
}
```

The exact metadata is `datasetKey = "db201-vi-50-v1"`, `language = "vi"`, `subjectCode = "DB201"`; external IDs are `DB201-VI-001` through `DB201-VI-050` with no gaps.

## 4. Chat Reliability And Variants

Architecture is frozen by [ADR 0010](../adr/0010-chat-turns-and-assistant-variants.md).

### Entity contract

`Domain.Entities.ChatMessage` adds nullable `int MessageIndex`, nullable `Guid InReplyToMessageId`, nullable `int VariantIndex`, `bool IsSelectedVariant`, reply navigation, and assistant-variant collection exactly as listed in ADR 0010. Logical message indices are 1-based: each user owns one slot and all variants of its assistant response share the immediately following slot. Assistant variant indices are 1-based.

### Domain DTO and persistence contract

`Domain.Contracts.DTOs.ResolvedChatMessage` adds:

```csharp
public required int? MessageIndex { get; init; }
public required Guid? InReplyToMessageId { get; init; }
public required int? VariantIndex { get; init; }
public required bool IsSelectedVariant { get; init; }
public ResolvedChatVariantNavigation? VariantNavigation { get; init; }
```

File: `Domain/Contracts/DTOs/ResolvedChatVariantNavigation.cs`  
Namespace: `Domain.Contracts.DTOs`

```csharp
public record ResolvedChatVariantOption
{
    public required Guid MessageId { get; init; }
    public required int VariantIndex { get; init; }
    public required bool IsSelected { get; init; }
    public required MessageStatus Status { get; init; }
}

public record ResolvedChatVariantNavigation
{
    public required Guid UserMessageId { get; init; }
    public required int CurrentVariantIndex { get; init; }
    public required int TotalVariantCount { get; init; }
    public required IReadOnlyList<ResolvedChatVariantOption> Variants { get; init; }
}
```

File: `Domain/Contracts/DTOs/ChatExchangeResult.cs`:

```csharp
public record ChatExchangeResult
{
    public required ResolvedChatMessage UserMessage { get; init; }
    public required ResolvedChatMessage AssistantMessage { get; init; }
}
```

`Domain.Contracts.IChatPersistenceService` retains current session/title/completion/failure members, removes the two separate message-creation members, and adds:

```csharp
Task<ChatExchangeResult> CreateExchangeAsync(
    Guid sessionId,
    string userContent,
    CancellationToken cancellationToken = default);

Task<ResolvedChatMessage> ResetFailedAssistantMessageAsync(
    Guid sessionId,
    Guid assistantMessageId,
    CancellationToken cancellationToken = default);

Task<ResolvedChatMessage> CreateAssistantVariantAsync(
    Guid sessionId,
    Guid completedAssistantMessageId,
    CancellationToken cancellationToken = default);

Task<ResolvedChatMessage?> GetAssistantVariantAsync(
    Guid sessionId,
    Guid assistantMessageId,
    CancellationToken cancellationToken = default);

Task<ResolvedChatMessage> SelectAssistantVariantAsync(
    Guid sessionId,
    Guid assistantMessageId,
    CancellationToken cancellationToken = default);
```

`CreateExchangeAsync` locks the owning `ChatSession` row, allocates the next two logical message indices, and creates the user plus selected assistant variant 1 in one transaction. `CreateAssistantVariantAsync` locks the replied-to user row before allocating `max(VariantIndex) + 1`. `GetSessionWithMessagesByIdAsync` returns user rows and selected assistant rows ordered by `MessageIndex`; when called by generation it must include the target pending assistant and exclude unselected history. `ChatGenerationCoordinator.BuildChatGenerationRequest` orders by `MessageIndex`, includes completed rows before the target assistant slot, filters assistant history to selected variants, and no longer compares `SentAt`.

`Domain.Contracts.IChatGenerationCoordinator.GenerateChatAsync(Guid sessionId, Guid assistantMessageId, Guid assistantMessageClientId, CancellationToken cancellationToken = default)` remains unchanged. Retry and regeneration both enqueue it. `BuildChatGenerationRequest` becomes turn-aware as specified in ADR 0010 and continues to resolve current subject configuration at generation time.

### Presentation DTOs

File: `PresentationLayer/DTOs/ChatVariantDtos.cs`  
Namespace: `Presentation.DTOs`

```csharp
public class ChatVariantOptionDto
{
    public Guid MessageId { get; set; }
    public int VariantIndex { get; set; }
    public bool IsSelected { get; set; }
    public MessageStatus Status { get; set; }
}

public class ChatVariantNavigationDto
{
    public Guid UserMessageId { get; set; }
    public int CurrentVariantIndex { get; set; }
    public int TotalVariantCount { get; set; }
    public List<ChatVariantOptionDto> Variants { get; set; } = [];
}

public class RetryAssistantMessageRequest
{
    public Guid MessageId { get; set; }
    public Guid AssistantMessageClientId { get; set; }
}

public class RegenerateAssistantMessageRequest
{
    public Guid MessageId { get; set; }
    public Guid AssistantMessageClientId { get; set; }
}

public class SelectAssistantVariantRequest
{
    public Guid MessageId { get; set; }
}

public class StartAssistantGenerationResponse
{
    public Guid AssistantMessageId { get; set; }
    public Guid AssistantMessageClientId { get; set; }
    public MessageStatus Status { get; set; }
    public ChatVariantNavigationDto VariantNavigation { get; set; } = null!;
}

public class DeleteChatSessionResponse
{
    public Guid SessionId { get; set; }
}
```

`Presentation.DTOs.ChatMessageDto` adds:

```csharp
public int? MessageIndex { get; set; }
public Guid? InReplyToMessageId { get; set; }
public int? VariantIndex { get; set; }
public bool IsSelectedVariant { get; set; }
public ChatVariantNavigationDto? VariantNavigation { get; set; }
```

The existing content, citations, status, and error properties describe only the DTO's current variant. Variant navigation is null for user/system messages and non-null for assistant messages.

### Chat handlers

All handlers remain on `Presentation.Pages.Chat.IndexModel` and verify the current user owns the session before mutation.

| Method | Route | Handler | Response and state |
|---|---|---|---|
| DELETE | `/chat?handler=Session&id={guid}` | `OnDeleteSessionAsync([FromQuery] Guid id, CancellationToken cxlTkn)` | `200 DeleteChatSessionResponse`; `404` absent/not owned |
| POST | `/chat?handler=Retry&sessionId={guid}` | `OnPostRetryAsync([FromQuery] Guid sessionId, [FromBody] RetryAssistantMessageRequest request, CancellationToken cxlTkn)` | `202 StartAssistantGenerationResponse`; only failed assistant; `400/404/409` |
| POST | `/chat?handler=Regenerate&sessionId={guid}` | `OnPostRegenerateAsync([FromQuery] Guid sessionId, [FromBody] RegenerateAssistantMessageRequest request, CancellationToken cxlTkn)` | `202 StartAssistantGenerationResponse`; only completed assistant; `400/404/409` |
| GET | `/chat?handler=Variant&sessionId={guid}&messageId={guid}` | `OnGetVariantAsync([FromQuery] Guid sessionId, [FromQuery] Guid messageId, CancellationToken cxlTkn)` | `200 ChatMessageDto`; no selection change; `404` |
| PUT | `/chat?handler=SelectedVariant&sessionId={guid}` | `OnPutSelectedVariantAsync([FromQuery] Guid sessionId, [FromBody] SelectAssistantVariantRequest request, CancellationToken cxlTkn)` | `200 ChatMessageDto`; no generation; `404/409` |

The existing generate handler uses `CreateExchangeAsync`; its public request/response remains compatible and its response receives the corrected existing property names `AssistantMessageSentAt` and `AssistantMessageStatus` already declared in `GenerateChatResponse`.

### SignalR client contract

`Presentation.Realtime.IAiChatClient` retains existing events and adds:

```csharp
Task AssistantVariantCreated(
    Guid userMessageId,
    Guid assistantMessageId,
    Guid assistantMessageClientId,
    ChatVariantNavigationDto variantNavigation);

Task AssistantVariantSelected(
    Guid userMessageId,
    Guid assistantMessageId,
    ChatVariantNavigationDto variantNavigation);
```

Retry reuses `GenerationStarted`, `ReceiveToken`, `GenerationCompleted`, and `GenerationFailed`. Regeneration sends `AssistantVariantCreated` before the generation job starts. Selection sends `AssistantVariantSelected` and never generation events. Session deletion uses existing `ResourceUpdate` with `ResourceType.ChatSession` and `ResourceAction.Deleted`; the originating client updates from HTTP and other clients reload/remove through the resource hub.

### Frontend state

A owns:

- `PresentationLayer/Pages/Chat/Index.cshtml`
- `PresentationLayer/Pages/Chat/Index.cshtml.cs`
- `PresentationLayer/Realtime/AiChatHub.cs`
- `PresentationLayer/Realtime/ChatGenerationCoordinator.cs`
- `PresentationLayer/wwwroot/js/chat/chat.js`
- `PresentationLayer/wwwroot/js/chat/chat-signalr.js`
- `PresentationLayer/wwwroot/js/chat/chat-templates.js`
- focused tests in new chat test files.

The current client already has `_variants`, `_activeVariant`, response navigation markup, and a regeneration stub. Replace that local-only shape with server identities:

```text
assistant message state
  current: full ChatMessageDto
  navigation: ChatVariantNavigationDto
  previewedMessageId: Guid
  selectedMessageId: Guid
```

Previous/next fetches the target compact option's full DTO through `Variant`; it does not regenerate or select. A visible “Use this response” action calls `SelectedVariant` when preview differs from selected. Retry is rendered only for failed assistant messages. Regenerate is rendered only for completed assistant messages. While a retry/regeneration is pending, duplicate mutation controls for that turn are disabled.

Fixtures:

- [chat-session-variants.json](fixtures/chat-session-variants.json)
- [chat-variant-response.json](fixtures/chat-variant-response.json)
- [chat-generation-start.json](fixtures/chat-generation-start.json)

## 5. Metrics Nullability Contract

These existing properties change before report implementation:

```csharp
// Domain.Contracts.DTOs.ChatGenerationMetrics
public required int? PromptTokens { get; init; }
public required int? CompletionTokens { get; init; }
public required long RetrievalTimeMs { get; init; }
public required long? TimeToFirstTokenMs { get; init; }
public required long TotalResponseTimeMs { get; init; }
public required double? TokensPerSecond { get; init; }

// Domain.Entities.ChatMessageGenerationMetrics
public int? PromptTokens { get; set; }
public int? CompletionTokens { get; set; }
public long RetrievalTimeMs { get; set; }
public long? TimeToFirstTokenMs { get; set; }
public long TotalResponseTimeMs { get; set; }
public double? TokensPerSecond { get; set; }
```

Title token fields become nullable as well. A zero is valid only when supplied by the provider or mathematically measured; missing provider data is null.

## Dependency Graph And Integration Sequence

```text
Owner contract-only foundation
  -> A chat implementation
  -> B AI configuration + experiment-create UI + dataset
  -> C reports + experiment-results UI

Owner AI/indexing/metrics backend
  -> B configuration handlers become live
  -> C report handlers become live

Owner experiment runner/evaluator/persistence
  -> B create handler becomes live
  -> C results/comparison handlers become live

Owner-only integration
  -> Program.cs registrations
  -> _AdminLayout.cshtml navigation links
  -> EF migrations and seed import
  -> final conflict resolution and end-to-end verification
```

Required integration order:

1. Owner publishes DTO/interface contract foundation and, after separate approval, chat entity/migration foundation.
2. A, B, and C branch from that exact foundation.
3. Owner completes AI configuration, chunking, metrics, reports, and experiment services without editing teammate-owned page/UI files.
4. Integrate A first because chat mappings and coordinator are sensitive to the foundation.
5. Integrate B, then C; their files are disjoint.
6. Owner alone edits `PresentationLayer/Program.cs`, `PresentationLayer/Pages/Admin/_AdminLayout.cshtml`, migrations, model snapshot, `Business.csproj`, and seed/import wiring.

## Test Ownership

- Owner: resolver precedence; three chunkers; strategy selection; indexed-value persistence; subject availability/lock and reindex recovery; usage nullability; report range/role/trend-subject projections, p95, distinct citation counting, and coverage; experiment preflight/orchestration/evaluator; migrations and backfill.
- A: owned session deletion; failed retry cleanup and same-ID behavior; completed regeneration creates next selected variant; preview without selection; selection without generation; SignalR event/state behavior; history excludes unselected/later variants.
- B: PageModel authorization and handler status codes with mocked owner services; JSON fixture normalization; inherited/override/effective rendering; form serialization; reindex and compatible/required/blocked experiment-preflight states; affected-document confirmation and navigation; dataset schema, count, language, ID uniqueness, non-empty ground truth, and final DB201 grounding.
- C: report range/role/trend-subject mapping; all-subject default and isolated trend filtering; null-token rendering; indexing donut, timelines, subject chart/table, health/hot-document rendering; every experiment progress state and exactly-two comparison selection.
