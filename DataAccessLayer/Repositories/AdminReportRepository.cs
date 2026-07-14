using DataAccess.Data;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed record AdminReportQuery(DateTime StartUtc, DateTime EndExclusiveUtc, ReportRoleFilter Role, int? TrendSubjectId);

public sealed record AdminReportRepositoryData
{
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

public class AdminReportRepository(EduChatAiDbContext context)
{
    private const int BangkokUtcOffsetHours = 7;
    protected readonly EduChatAiDbContext Context = context;

    public virtual Task<bool> SubjectExistsAsync(int subjectId, CancellationToken cxlTkn = default) =>
        Context.Subjects.AsNoTracking().AnyAsync(subject => subject.Id == subjectId, cxlTkn);

    public virtual Task<DateTime?> GetEarliestChatEventUtcAsync(ReportRoleFilter role, CancellationToken cxlTkn = default) =>
        ApplyRoleFilter(Context.ChatMessages.AsNoTracking()
                .Where(message => message.ChatRole == ChatRole.User || message.ChatRole == ChatRole.Assistant), role)
            .MinAsync(message => (DateTime?)message.SentAt, cxlTkn);

    public virtual async Task<AdminReportRepositoryData> GetDashboardDataAsync(AdminReportQuery query, CancellationToken cxlTkn = default)
    {
        var messages = ApplyRoleFilter(Context.ChatMessages.AsNoTracking()
            .Where(message => message.SentAt >= query.StartUtc && message.SentAt < query.EndExclusiveUtc), query.Role);
        var userMessages = messages.Where(message => message.ChatRole == ChatRole.User);
        var assistantMessages = messages.Where(message => message.ChatRole == ChatRole.Assistant);
        var completedMessages = assistantMessages.Where(message => message.Status == MessageStatus.Completed);
        var terminalMessages = assistantMessages.Where(message => message.Status == MessageStatus.Completed || message.Status == MessageStatus.Failed);

        var trendSubjectOptions = await Context.Subjects.AsNoTracking()
            .OrderBy(subject => subject.Code).ThenBy(subject => subject.Id)
            .Select(subject => new ReportSubjectOptionDto { SubjectId = subject.Id, SubjectCode = subject.Code, SubjectName = subject.Name })
            .ToListAsync(cxlTkn);

        var uniqueActiveUserCount = await userMessages.Select(message => message.ChatSession.UserId).Distinct().LongCountAsync(cxlTkn);
        var activeSessionCount = await userMessages.Select(message => message.ChatSessionId).Distinct().LongCountAsync(cxlTkn);
        var completedCount = await completedMessages.LongCountAsync(cxlTkn);
        var terminalCount = await terminalMessages.LongCountAsync(cxlTkn);
        var citationEligibleCount = await completedMessages.LongCountAsync(message => message.ChatSession.SubjectId != null, cxlTkn);
        var citationCoveredCount = await completedMessages.LongCountAsync(message => message.ChatSession.SubjectId != null && message.Citations.Any(), cxlTkn);

        var completedRows = await completedMessages.Select(message => new CompletedGenerationRow
        {
            SentAt = message.SentAt,
            SubjectId = message.ChatSession.SubjectId,
            SubjectCode = message.ChatSession.Subject == null ? null : message.ChatSession.Subject.Code,
            SubjectName = message.ChatSession.Subject == null ? null : message.ChatSession.Subject.Name,
            PromptTokens = message.GenerationMetrics == null ? null : message.GenerationMetrics.PromptTokens,
            CompletionTokens = message.GenerationMetrics == null ? null : message.GenerationMetrics.CompletionTokens,
            TotalResponseTimeMs = message.GenerationMetrics == null ? null : message.GenerationMetrics.TotalResponseTimeMs,
            ContextChunkCount = message.GenerationMetrics == null ? null : message.GenerationMetrics.ContextChunkCount,
        }).ToListAsync(cxlTkn);

        var failedRows = await assistantMessages
            .Where(message => message.Status == MessageStatus.Failed && message.ChatSession.SubjectId != null)
            .Select(message => new FailedGenerationRow { SubjectId = message.ChatSession.SubjectId!.Value })
            .ToListAsync(cxlTkn);

        var userActivity = await userMessages.GroupBy(message => new
            {
                message.ChatSession.SubjectId,
                SubjectCode = message.ChatSession.Subject == null ? null : message.ChatSession.Subject.Code,
                SubjectName = message.ChatSession.Subject == null ? null : message.ChatSession.Subject.Name,
            })
            .Select(group => new SubjectActivityRow
            {
                SubjectId = group.Key.SubjectId,
                SubjectCode = group.Key.SubjectCode,
                SubjectName = group.Key.SubjectName,
                UniqueActiveUserCount = group.Select(message => message.ChatSession.UserId).Distinct().Count(),
                ActiveSessionCount = group.Select(message => message.ChatSessionId).Distinct().Count(),
            })
            .ToListAsync(cxlTkn);

        var trendCompletedMessages = query.TrendSubjectId.HasValue
            ? completedMessages.Where(message => message.ChatSession.SubjectId == query.TrendSubjectId)
            : completedMessages;
        var assistantGenerations = await trendCompletedMessages
            .GroupBy(message => message.SentAt.AddHours(BangkokUtcOffsetHours).Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .OrderBy(group => group.Date)
            .ToListAsync(cxlTkn);

        IReadOnlyList<CompletedGenerationRow> trendRows = query.TrendSubjectId.HasValue
            ? completedRows.Where(row => row.SubjectId == query.TrendSubjectId.Value).ToArray()
            : completedRows;
        var tokenUsage = trendRows.GroupBy(row => BangkokDate(row.SentAt)).OrderBy(group => group.Key)
            .Select(group => new DailyTokenUsageDto { Date = group.Key, TokenMeasurement = BuildTokenMeasurement(group) })
            .ToArray();
        var responseLatency = trendRows.GroupBy(row => BangkokDate(row.SentAt)).OrderBy(group => group.Key)
            .Select(group => new DailyResponseLatencyDto { Date = group.Key, P95TotalResponseTimeMs = NearestRankP95(group.Select(row => row.TotalResponseTimeMs)) })
            .ToArray();

        var subjectUsage = BuildSubjectUsage(userActivity, completedRows);
        var subjectsNeedingAttention = BuildSubjectHealth(completedRows, failedRows);
        var indexingStatus = new IndexingStatusSummaryDto
        {
            IndexedDocumentCount = await Context.Documents.AsNoTracking().CountAsync(document => document.Status == DocumentStatus.Indexed, cxlTkn),
            ProcessingDocumentCount = await Context.Documents.AsNoTracking().CountAsync(document => document.Status != DocumentStatus.Indexed && document.Status != DocumentStatus.Failed, cxlTkn),
            FailedDocumentCount = await Context.Documents.AsNoTracking().CountAsync(document => document.Status == DocumentStatus.Failed, cxlTkn),
        };
        var hotDocuments = await completedMessages
            .SelectMany(message => message.Citations.Select(citation => new
            {
                AnswerId = message.Id,
                DocumentId = citation.Chunk.DocumentId,
                DocumentTitle = citation.Chunk.Document.Title,
                citation.Chunk.Document.SubjectId,
                SubjectCode = citation.Chunk.Document.Subject.Code,
            }))
            .Distinct()
            .GroupBy(item => new { item.DocumentId, item.DocumentTitle, item.SubjectId, item.SubjectCode })
            .Select(group => new HotDocumentDto
            {
                DocumentId = group.Key.DocumentId,
                DocumentTitle = group.Key.DocumentTitle,
                SubjectId = group.Key.SubjectId,
                SubjectCode = group.Key.SubjectCode,
                CitedAnswerCount = group.Count(),
            })
            .OrderByDescending(document => document.CitedAnswerCount)
            .ThenBy(document => document.DocumentTitle)
            .ThenBy(document => document.DocumentId)
            .Take(10)
            .ToListAsync(cxlTkn);

        return new AdminReportRepositoryData
        {
            TrendSubjectOptions = trendSubjectOptions,
            Kpis = new AdminReportKpiDto
            {
                UniqueActiveUserCount = uniqueActiveUserCount,
                ActiveSessionCount = activeSessionCount,
                CompletedAssistantGenerationCount = completedCount,
                TerminalGenerationCount = terminalCount,
                GenerationSuccessRatePercent = terminalCount == 0 ? null : 100d * completedCount / terminalCount,
                CitationCoveragePercent = citationEligibleCount == 0 ? null : 100d * citationCoveredCount / citationEligibleCount,
                P95TotalResponseTimeMs = NearestRankP95(completedRows.Select(row => row.TotalResponseTimeMs)),
                TokenMeasurement = BuildTokenMeasurement(completedRows),
            },
            AssistantGenerations = assistantGenerations.Select(group => new DailyAssistantGenerationDto
            {
                Date = DateOnly.FromDateTime(group.Date),
                CompletedAssistantGenerationCount = group.Count,
            }).ToArray(),
            TokenUsage = tokenUsage,
            ResponseLatency = responseLatency,
            SubjectUsage = subjectUsage,
            SubjectsNeedingAttention = subjectsNeedingAttention,
            IndexingStatus = indexingStatus,
            HotDocuments = hotDocuments,
        };
    }

    private static IQueryable<ChatMessage> ApplyRoleFilter(IQueryable<ChatMessage> messages, ReportRoleFilter role)
    {
        if (role == ReportRoleFilter.All) return messages;
        var roleName = role.ToString();
        return messages.Where(message => message.ChatSession.User.UserRoles.Any(userRole => userRole.Role.Name == roleName));
    }

    private static IReadOnlyList<SubjectUsageDto> BuildSubjectUsage(IReadOnlyList<SubjectActivityRow> activity, IReadOnlyList<CompletedGenerationRow> generations)
    {
        var activityBySubject = activity.ToDictionary(row => new SubjectKey(row.SubjectId, row.SubjectCode, row.SubjectName));
        var generationsBySubject = generations.GroupBy(row => new SubjectKey(row.SubjectId, row.SubjectCode, row.SubjectName)).ToDictionary(group => group.Key, group => group.ToArray());
        var subjects = activityBySubject.Keys.Concat(generationsBySubject.Keys).Distinct().ToArray();
        return subjects.Select(subject =>
            {
                activityBySubject.TryGetValue(subject, out var activityRow);
                generationsBySubject.TryGetValue(subject, out var generationRows);
                return new SubjectUsageDto
                {
                    SubjectId = subject.SubjectId,
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName ?? "All subjects",
                    UniqueActiveUserCount = activityRow?.UniqueActiveUserCount ?? 0,
                    ActiveSessionCount = activityRow?.ActiveSessionCount ?? 0,
                    CompletedAssistantGenerationCount = generationRows?.Length ?? 0,
                    TokenMeasurement = BuildTokenMeasurement(generationRows ?? []),
                };
            })
            .OrderByDescending(row => row.CompletedAssistantGenerationCount)
            .ThenByDescending(row => row.ActiveSessionCount)
            .ThenBy(row => row.SubjectCode)
            .ThenBy(row => row.SubjectId)
            .ToArray();
    }

    private static IReadOnlyList<SubjectHealthDto> BuildSubjectHealth(IReadOnlyList<CompletedGenerationRow> completed, IReadOnlyList<FailedGenerationRow> failed)
    {
        var failedCounts = failed.GroupBy(row => row.SubjectId).ToDictionary(group => group.Key, group => group.Count());
        return completed.Where(row => row.SubjectId.HasValue).GroupBy(row => row.SubjectId!.Value)
            .Where(group => group.Count() >= 10)
            .Select(group =>
            {
                var first = group.First();
                var failureCount = failedCounts.GetValueOrDefault(group.Key);
                return new SubjectHealthDto
                {
                    SubjectId = group.Key,
                    SubjectCode = first.SubjectCode!,
                    SubjectName = first.SubjectName!,
                    CompletedAssistantGenerationCount = group.Count(),
                    NoContextRatePercent = 100d * group.Count(row => row.ContextChunkCount == 0) / group.Count(),
                    GenerationFailureRatePercent = 100d * failureCount / (group.Count() + failureCount),
                    P95TotalResponseTimeMs = NearestRankP95(group.Select(row => row.TotalResponseTimeMs)),
                };
            })
            .OrderByDescending(row => row.NoContextRatePercent)
            .ThenByDescending(row => row.GenerationFailureRatePercent)
            .ThenBy(row => row.SubjectCode)
            .ThenBy(row => row.SubjectId)
            .ToArray();
    }

    private static TokenMeasurementDto BuildTokenMeasurement(IEnumerable<CompletedGenerationRow> source)
    {
        var rows = source as IReadOnlyCollection<CompletedGenerationRow> ?? source.ToArray();
        var measured = rows.Where(row => row.PromptTokens.HasValue && row.CompletionTokens.HasValue).ToArray();
        if (rows.Count == 0) return new TokenMeasurementDto
        {
            MeasuredPromptTokens = 0,
            MeasuredCompletionTokens = 0,
            MeasuredTotalTokens = 0,
            MeasuredMessageCount = 0,
            EligibleMessageCount = 0,
            CoveragePercent = 100,
        };
        if (measured.Length == 0) return new TokenMeasurementDto
        {
            MeasuredPromptTokens = null,
            MeasuredCompletionTokens = null,
            MeasuredTotalTokens = null,
            MeasuredMessageCount = 0,
            EligibleMessageCount = rows.Count,
            CoveragePercent = 0,
        };

        var prompt = measured.Sum(row => (long)row.PromptTokens!.Value);
        var completion = measured.Sum(row => (long)row.CompletionTokens!.Value);
        return new TokenMeasurementDto
        {
            MeasuredPromptTokens = prompt,
            MeasuredCompletionTokens = completion,
            MeasuredTotalTokens = prompt + completion,
            MeasuredMessageCount = measured.Length,
            EligibleMessageCount = rows.Count,
            CoveragePercent = 100d * measured.Length / rows.Count,
        };
    }

    private static long? NearestRankP95(IEnumerable<long?> source)
    {
        var measured = source.Where(value => value.HasValue).Select(value => value!.Value).Order().ToArray();
        if (measured.Length == 0) return null;
        return measured[(int)Math.Ceiling(measured.Length * 0.95d) - 1];
    }

    private static DateOnly BangkokDate(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(BangkokUtcOffsetHours));

    private sealed class CompletedGenerationRow
    {
        public required DateTime SentAt { get; init; }
        public required int? SubjectId { get; init; }
        public required string? SubjectCode { get; init; }
        public required string? SubjectName { get; init; }
        public required int? PromptTokens { get; init; }
        public required int? CompletionTokens { get; init; }
        public required long? TotalResponseTimeMs { get; init; }
        public required int? ContextChunkCount { get; init; }
    }

    private sealed class FailedGenerationRow { public required int SubjectId { get; init; } }

    private readonly record struct SubjectKey(int? SubjectId, string? SubjectCode, string? SubjectName);

    private sealed class SubjectActivityRow
    {
        public required int? SubjectId { get; init; }
        public required string? SubjectCode { get; init; }
        public required string? SubjectName { get; init; }
        public required int UniqueActiveUserCount { get; init; }
        public required int ActiveSessionCount { get; init; }
    }
}
