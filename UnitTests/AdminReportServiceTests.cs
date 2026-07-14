using Business.Services.Reports;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Moq;

namespace UnitTests;

[TestFixture]
public sealed class AdminReportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 3, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Last7Days_UsesBangkokBoundariesAndZeroFillsEveryLocalDay()
    {
        var repository = new StubAdminReportRepository
        {
            Data = EmptyData() with
            {
                AssistantGenerations = [new() { Date = new DateOnly(2026, 7, 10), CompletedAssistantGenerationCount = 2 }],
                TokenUsage = [new() { Date = new DateOnly(2026, 7, 10), TokenMeasurement = Tokens(10, 4, 1, 2) }],
                ResponseLatency = [new() { Date = new DateOnly(2026, 7, 10), P95TotalResponseTimeMs = 900 }],
            },
        };
        var result = await Create(repository).GetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FromUtc, Is.EqualTo(new DateTime(2026, 7, 8, 17, 0, 0, DateTimeKind.Utc)));
            Assert.That(result.ToUtc, Is.EqualTo(new DateTime(2026, 7, 15, 16, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999)));
            Assert.That(result.AssistantGenerations.Select(e => e.Date), Is.EqualTo(Enumerable.Range(9, 7).Select(day => new DateOnly(2026, 7, day))));
            Assert.That(result.AssistantGenerations.Single(e => e.Date == new DateOnly(2026, 7, 10)).CompletedAssistantGenerationCount, Is.EqualTo(2));
            Assert.That(result.TokenUsage.Single(e => e.Date == new DateOnly(2026, 7, 9)).TokenMeasurement, Is.EqualTo(Tokens(0, 0, 0, 0)));
            Assert.That(result.ResponseLatency.Single(e => e.Date == new DateOnly(2026, 7, 9)).P95TotalResponseTimeMs, Is.Null);
            Assert.That(repository.LastQuery!.StartUtc, Is.EqualTo(result.FromUtc));
            Assert.That(repository.LastQuery.EndExclusiveUtc, Is.EqualTo(result.ToUtc.AddTicks(1)));
        }
    }

    [Test]
    public async Task AllTimeWithoutActivity_ReturnsNullFromAndOneDeterministicZeroDay()
    {
        var repository = new StubAdminReportRepository { EarliestActivityUtc = null, Data = EmptyData() };

        var result = await Create(repository).GetDashboardAsync(ReportRange.AllTime, ReportRoleFilter.All, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FromUtc, Is.Null);
            Assert.That(result.AssistantGenerations, Has.Count.EqualTo(1));
            Assert.That(result.AssistantGenerations[0].Date, Is.EqualTo(new DateOnly(2026, 7, 15)));
            Assert.That(result.AssistantGenerations[0].CompletedAssistantGenerationCount, Is.Zero);
            Assert.That(repository.LastQuery!.StartUtc, Is.EqualTo(new DateTime(2026, 7, 14, 17, 0, 0, DateTimeKind.Utc)));
        }
    }

    [Test]
    public async Task Last30Days_ReturnsExactlyThirtyBangkokDates()
    {
        var repository = new StubAdminReportRepository { Data = EmptyData() };

        var result = await Create(repository).GetDashboardAsync(ReportRange.Last30Days, ReportRoleFilter.All, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.AssistantGenerations, Has.Count.EqualTo(30));
            Assert.That(result.AssistantGenerations[0].Date, Is.EqualTo(new DateOnly(2026, 6, 16)));
            Assert.That(result.AssistantGenerations[^1].Date, Is.EqualTo(new DateOnly(2026, 7, 15)));
            Assert.That(repository.LastQuery!.StartUtc, Is.EqualTo(new DateTime(2026, 6, 15, 17, 0, 0, DateTimeKind.Utc)));
            Assert.That(repository.LastQuery.EndExclusiveUtc, Is.EqualTo(new DateTime(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc)));
        }
    }

    [Test]
    public async Task AllTimeWithActivity_StartsAtEarliestBangkokDayBoundary()
    {
        var repository = new StubAdminReportRepository
        {
            EarliestActivityUtc = new DateTime(2026, 7, 9, 16, 30, 0, DateTimeKind.Utc),
            Data = EmptyData(),
        };

        var result = await Create(repository).GetDashboardAsync(ReportRange.AllTime, ReportRoleFilter.Student, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FromUtc, Is.EqualTo(new DateTime(2026, 7, 8, 17, 0, 0, DateTimeKind.Utc)));
            Assert.That(result.AssistantGenerations.Select(e => e.Date), Is.EqualTo(Enumerable.Range(9, 7).Select(day => new DateOnly(2026, 7, day))));
            Assert.That(repository.LastQuery!.Role, Is.EqualTo(ReportRoleFilter.Student));
        }
    }

    [Test]
    public void UnknownTrendSubject_ThrowsNotFoundBeforeDashboardQueries()
    {
        var repository = new StubAdminReportRepository { SubjectExists = false, Data = EmptyData() };

        Assert.ThrowsAsync<EntityNotFoundException>(() => Create(repository).GetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, 999));
        Assert.That(repository.LastQuery, Is.Null);
    }

    [Test]
    public void InvalidEnum_ThrowsBadRequest()
    {
        var repository = new StubAdminReportRepository { Data = EmptyData() };

        Assert.ThrowsAsync<BadRequestException>(() => Create(repository).GetDashboardAsync((ReportRange)999, ReportRoleFilter.All, null));
        Assert.ThrowsAsync<BadRequestException>(() => Create(repository).GetDashboardAsync(ReportRange.Last7Days, (ReportRoleFilter)999, null));
    }

    private static AdminReportService Create(StubAdminReportRepository repository)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(e => e.AdminReports).Returns(repository);
        return new AdminReportService(unitOfWork.Object, new FixedTimeProvider(Now));
    }

    private static AdminReportRepositoryData EmptyData() => new()
    {
        TrendSubjectOptions = [],
        Kpis = new AdminReportKpiDto
        {
            UniqueActiveUserCount = 0,
            ActiveSessionCount = 0,
            CompletedAssistantGenerationCount = 0,
            TerminalGenerationCount = 0,
            GenerationSuccessRatePercent = null,
            CitationCoveragePercent = null,
            P95TotalResponseTimeMs = null,
            TokenMeasurement = Tokens(0, 0, 0, 0),
        },
        AssistantGenerations = [],
        TokenUsage = [],
        ResponseLatency = [],
        SubjectUsage = [],
        SubjectsNeedingAttention = [],
        IndexingStatus = new IndexingStatusSummaryDto { IndexedDocumentCount = 0, ProcessingDocumentCount = 0, FailedDocumentCount = 0 },
        HotDocuments = [],
    };

    private static TokenMeasurementDto Tokens(long? prompt, long? completion, int measured, int eligible) => new()
    {
        MeasuredPromptTokens = prompt,
        MeasuredCompletionTokens = completion,
        MeasuredTotalTokens = prompt.HasValue && completion.HasValue ? prompt + completion : null,
        MeasuredMessageCount = measured,
        EligibleMessageCount = eligible,
        CoveragePercent = eligible == 0 ? 100 : 100d * measured / eligible,
    };

    private sealed class StubAdminReportRepository : AdminReportRepository
    {
        public bool SubjectExists { get; set; } = true;
        public DateTime? EarliestActivityUtc { get; set; }
        public required AdminReportRepositoryData Data { get; init; }
        public AdminReportQuery? LastQuery { get; private set; }

        public StubAdminReportRepository() : base(null!) { }

        public override Task<bool> SubjectExistsAsync(int subjectId, CancellationToken cxlTkn = default) => Task.FromResult(SubjectExists);
        public override Task<DateTime?> GetEarliestChatEventUtcAsync(ReportRoleFilter role, CancellationToken cxlTkn = default) => Task.FromResult(EarliestActivityUtc);
        public override Task<AdminReportRepositoryData> GetDashboardDataAsync(AdminReportQuery query, CancellationToken cxlTkn = default)
        {
            LastQuery = query;
            return Task.FromResult(Data);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
