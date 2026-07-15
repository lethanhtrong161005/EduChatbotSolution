using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;

namespace Business.Services.Reports;

public sealed class AdminReportService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IAdminReportService
{
    private static readonly TimeZoneInfo BangkokTimeZone = ResolveBangkokTimeZone();
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<AdminReportDashboardDto> GetDashboardAsync(ReportRange range, ReportRoleFilter role, int? trendSubjectId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(range)) throw new BadRequestException("Invalid report range.", nameof(range));
        if (!Enum.IsDefined(role)) throw new BadRequestException("Invalid report role.", nameof(role));

        // trendSubjectId == 0 => Flexible-subject sessions
        if (trendSubjectId.HasValue && trendSubjectId.Value != 0 && !await _unitOfWork.AdminReports.SubjectExistsAsync(trendSubjectId.Value, cancellationToken))
            throw new EntityNotFoundException(trendSubjectId.Value);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, BangkokTimeZone));
        DateTime? exposedStartUtc;
        DateOnly firstDate;

        if (range == ReportRange.AllTime)
        {
            var earliest = await _unitOfWork.AdminReports.GetEarliestChatEventUtcAsync(role, cancellationToken);
            firstDate = earliest.HasValue ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(earliest.Value, DateTimeKind.Utc), BangkokTimeZone)) : today;
            exposedStartUtc = earliest.HasValue ? ToUtc(firstDate) : null;
        }
        else
        {
            firstDate = today.AddDays(range == ReportRange.Last7Days ? -6 : -29);
            exposedStartUtc = ToUtc(firstDate);
        }

        var queryStartUtc = exposedStartUtc ?? ToUtc(firstDate);
        var endExclusiveUtc = ToUtc(today.AddDays(1));
        var data = await _unitOfWork.AdminReports.GetDashboardDataAsync(new AdminReportQuery(queryStartUtc, endExclusiveUtc, role, trendSubjectId), cancellationToken);

        var dates = EnumerateDates(firstDate, today).ToArray();
        var generations = data.AssistantGenerations.ToDictionary(e => e.Date);
        var tokens = data.TokenUsage.ToDictionary(e => e.Date);
        var latency = data.ResponseLatency.ToDictionary(e => e.Date);

        return new AdminReportDashboardDto
        {
            Range = range,
            Role = role,
            TrendSubjectId = trendSubjectId,
            FromUtc = exposedStartUtc,
            ToUtc = endExclusiveUtc.AddTicks(-1),
            TrendSubjectOptions = data.TrendSubjectOptions,
            Kpis = data.Kpis,
            AssistantGenerations = dates.Select(date => generations.GetValueOrDefault(date) ?? new DailyAssistantGenerationDto { Date = date, CompletedAssistantGenerationCount = 0 }).ToArray(),
            TokenUsage = dates.Select(date => tokens.GetValueOrDefault(date) ?? new DailyTokenUsageDto { Date = date, TokenMeasurement = EmptyTokenMeasurement() }).ToArray(),
            ResponseLatency = dates.Select(date => latency.GetValueOrDefault(date) ?? new DailyResponseLatencyDto { Date = date, P95TotalResponseTimeMs = null }).ToArray(),
            SubjectUsage = data.SubjectUsage,
            SubjectsNeedingAttention = data.SubjectsNeedingAttention,
            IndexingStatus = data.IndexingStatus,
            HotDocuments = data.HotDocuments,
        };
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly first, DateOnly last)
    {
        for (var date = first; date <= last; date = date.AddDays(1)) yield return date;
    }

    private static DateTime ToUtc(DateOnly date) => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), BangkokTimeZone);

    private static TokenMeasurementDto EmptyTokenMeasurement() => new()
    {
        MeasuredPromptTokens = 0,
        MeasuredCompletionTokens = 0,
        MeasuredTotalTokens = 0,
        MeasuredMessageCount = 0,
        EligibleMessageCount = 0,
        CoveragePercent = 100,
    };

    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        foreach (var id in new[] { "Asia/Bangkok", "SE Asia Standard Time" })
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        throw new TimeZoneNotFoundException("Neither Asia/Bangkok nor SE Asia Standard Time is available.");
    }
}
