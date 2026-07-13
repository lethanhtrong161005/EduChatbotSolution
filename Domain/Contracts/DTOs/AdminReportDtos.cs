namespace Domain.Contracts.DTOs;

public enum ReportRange { Last7Days = 0, Last30Days = 1, AllTime = 2 }
public enum ReportRoleFilter { All = 0, Student = 1, Lecturer = 2, Admin = 3 }

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

public record ReportSubjectOptionDto { public required int SubjectId { get; init; } public required string SubjectCode { get; init; } public required string SubjectName { get; init; } }
public record DailyAssistantGenerationDto { public required DateOnly Date { get; init; } public required int CompletedAssistantGenerationCount { get; init; } }
public record DailyTokenUsageDto { public required DateOnly Date { get; init; } public required TokenMeasurementDto TokenMeasurement { get; init; } }
public record DailyResponseLatencyDto { public required DateOnly Date { get; init; } public required long? P95TotalResponseTimeMs { get; init; } }

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

public record IndexingStatusSummaryDto { public required int IndexedDocumentCount { get; init; } public required int ProcessingDocumentCount { get; init; } public required int FailedDocumentCount { get; init; } }
public record HotDocumentDto { public required Guid DocumentId { get; init; } public required string DocumentTitle { get; init; } public required int SubjectId { get; init; } public required string SubjectCode { get; init; } public required int CitedAnswerCount { get; init; } }

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
