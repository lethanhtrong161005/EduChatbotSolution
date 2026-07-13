using Domain.Contracts;
using Domain.Contracts.DTOs;

namespace Presentation.Services.Stubs;

/// <summary>
/// STUB — Frontend preview only. Remove when the real IAdminReportService is registered.
/// All data is hard-coded inline — no filesystem or database dependency.
/// </summary>
internal sealed class StubAdminReportService : IAdminReportService
{
    public Task<AdminReportDashboardDto> GetDashboardAsync(
        ReportRange range,
        ReportRoleFilter role,
        int? trendSubjectId,
        CancellationToken cancellationToken = default)
    {
        var dto = new AdminReportDashboardDto
        {
            Range = range,
            Role = role,
            TrendSubjectId = trendSubjectId,
            FromUtc = DateTime.UtcNow.AddDays(-7),
            ToUtc = DateTime.UtcNow,
            TrendSubjectOptions = new List<ReportSubjectOptionDto>
            {
                new() { SubjectId = 3, SubjectCode = "DB201", SubjectName = "Database Systems" },
                new() { SubjectId = 2, SubjectCode = "AI301", SubjectName = "Artificial Intelligence" },
            }.AsReadOnly(),
            Kpis = new AdminReportKpiDto
            {
                UniqueActiveUserCount = 74,
                ActiveSessionCount = 83,
                CompletedAssistantGenerationCount = 216,
                TerminalGenerationCount = 220,
                GenerationSuccessRatePercent = 98.18,
                CitationCoveragePercent = 82.14,
                P95TotalResponseTimeMs = 2380,
                TokenMeasurement = new TokenMeasurementDto
                {
                    MeasuredPromptTokens = 42160,
                    MeasuredCompletionTokens = 6059,
                    MeasuredTotalTokens = 48219,
                    MeasuredMessageCount = 190,
                    EligibleMessageCount = 216,
                    CoveragePercent = 87.96,
                },
            },
            AssistantGenerations = new List<DailyAssistantGenerationDto>
            {
                new() { Date = new DateOnly(2026, 7, 7),  CompletedAssistantGenerationCount = 21 },
                new() { Date = new DateOnly(2026, 7, 8),  CompletedAssistantGenerationCount = 0  },
                new() { Date = new DateOnly(2026, 7, 9),  CompletedAssistantGenerationCount = 35 },
                new() { Date = new DateOnly(2026, 7, 10), CompletedAssistantGenerationCount = 42 },
                new() { Date = new DateOnly(2026, 7, 11), CompletedAssistantGenerationCount = 31 },
                new() { Date = new DateOnly(2026, 7, 12), CompletedAssistantGenerationCount = 39 },
                new() { Date = new DateOnly(2026, 7, 13), CompletedAssistantGenerationCount = 48 },
            }.AsReadOnly(),
            TokenUsage = new List<DailyTokenUsageDto>
            {
                new() { Date = new DateOnly(2026, 7, 7),  TokenMeasurement = Tm(3540,  560,  4100,  18, 21, 85.71) },
                new() { Date = new DateOnly(2026, 7, 8),  TokenMeasurement = Tm(0, 0, 0, 0, 0, 100.0) },
                new() { Date = new DateOnly(2026, 7, 9),  TokenMeasurement = Tm(6610,  940,  7550,  31, 35, 88.57) },
                new() { Date = new DateOnly(2026, 7, 10), TokenMeasurement = Tm(7900, 1110,  9010,  37, 42, 88.10) },
                new() { Date = new DateOnly(2026, 7, 11), TokenMeasurement = Tm(5970,  860,  6830,  27, 31, 87.10) },
                new() { Date = new DateOnly(2026, 7, 12), TokenMeasurement = Tm(7780, 1100,  8880,  34, 39, 87.18) },
                new() { Date = new DateOnly(2026, 7, 13), TokenMeasurement = Tm(10360,1489, 11849, 43, 48, 89.58) },
            }.AsReadOnly(),
            ResponseLatency = new List<DailyResponseLatencyDto>
            {
                new() { Date = new DateOnly(2026, 7, 7),  P95TotalResponseTimeMs = 2190 },
                new() { Date = new DateOnly(2026, 7, 8),  P95TotalResponseTimeMs = null },
                new() { Date = new DateOnly(2026, 7, 9),  P95TotalResponseTimeMs = 2260 },
                new() { Date = new DateOnly(2026, 7, 10), P95TotalResponseTimeMs = 2310 },
                new() { Date = new DateOnly(2026, 7, 11), P95TotalResponseTimeMs = 2380 },
                new() { Date = new DateOnly(2026, 7, 12), P95TotalResponseTimeMs = 2240 },
                new() { Date = new DateOnly(2026, 7, 13), P95TotalResponseTimeMs = 2350 },
            }.AsReadOnly(),
            SubjectUsage = new List<SubjectUsageDto>
            {
                new()
                {
                    SubjectId = 3, SubjectCode = "DB201", SubjectName = "Database Systems",
                    UniqueActiveUserCount = 51, ActiveSessionCount = 57,
                    CompletedAssistantGenerationCount = 126,
                    TokenMeasurement = Tm(25510, 3680, 29190, 113, 126, 89.68),
                },
                new()
                {
                    SubjectId = 2, SubjectCode = "AI301", SubjectName = "Artificial Intelligence",
                    UniqueActiveUserCount = 31, ActiveSessionCount = 34,
                    CompletedAssistantGenerationCount = 70,
                    TokenMeasurement = Tm(14020, 2009, 16029, 62, 70, 88.57),
                },
                new()
                {
                    SubjectId = null, SubjectCode = null, SubjectName = "All subjects",
                    UniqueActiveUserCount = 12, ActiveSessionCount = 13,
                    CompletedAssistantGenerationCount = 20,
                    TokenMeasurement = Tm(2630, 370, 3000, 15, 20, 75.0),
                },
            }.AsReadOnly(),
            SubjectsNeedingAttention = new List<SubjectHealthDto>
            {
                new()
                {
                    SubjectId = 2, SubjectCode = "AI301", SubjectName = "Artificial Intelligence",
                    CompletedAssistantGenerationCount = 70,
                    NoContextRatePercent = 15.71,
                    GenerationFailureRatePercent = 4.11,
                    P95TotalResponseTimeMs = 2460,
                },
                new()
                {
                    SubjectId = 3, SubjectCode = "DB201", SubjectName = "Database Systems",
                    CompletedAssistantGenerationCount = 126,
                    NoContextRatePercent = 7.94,
                    GenerationFailureRatePercent = 0.79,
                    P95TotalResponseTimeMs = 2290,
                },
            }.AsReadOnly(),
            IndexingStatus = new IndexingStatusSummaryDto
            {
                IndexedDocumentCount = 42,
                ProcessingDocumentCount = 3,
                FailedDocumentCount = 2,
            },
            HotDocuments = new List<HotDocumentDto>
            {
                new() { DocumentId = Guid.Parse("10000000-0000-0000-0000-000000000001"), DocumentTitle = "First Normal Form",  SubjectId = 3, SubjectCode = "DB201", CitedAnswerCount = 61 },
                new() { DocumentId = Guid.Parse("10000000-0000-0000-0000-000000000002"), DocumentTitle = "Database Indexes",   SubjectId = 3, SubjectCode = "DB201", CitedAnswerCount = 54 },
            }.AsReadOnly(),
        };

        return Task.FromResult(dto);
    }

    private static TokenMeasurementDto Tm(
        long prompt, long completion, long total,
        int measured, int eligible, double coverage)
        => new()
        {
            MeasuredPromptTokens = prompt,
            MeasuredCompletionTokens = completion,
            MeasuredTotalTokens = total,
            MeasuredMessageCount = measured,
            EligibleMessageCount = eligible,
            CoveragePercent = coverage,
        };
}
