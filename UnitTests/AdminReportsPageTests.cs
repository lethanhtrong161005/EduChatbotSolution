using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Pages.Admin;

namespace UnitTests;

/// <summary>
/// Unit tests for <see cref="ReportsModel"/>.
/// Covers Admin authorization, all three range values, role filter mapping,
/// trend-subject filtering behavior, token coverage semantics, and edge cases.
/// </summary>
[TestFixture]
public class AdminReportsPageTests
{
    private Mock<IAdminReportService> _serviceMock = null!;
    private ReportsModel _model = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IAdminReportService>();
        _model = new ReportsModel(_serviceMock.Object);

        var httpContext = new DefaultHttpContext();
        _model.PageContext = new PageContext { HttpContext = httpContext };
    }

    // ── Authorization ─────────────────────────────────────────

    [Test]
    public void ReportsModel_HasAdminAuthorizeAttribute()
    {
        var attrs = typeof(ReportsModel).GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true);
        Assert.That(attrs, Is.Not.Empty, "ReportsModel must be decorated with [Authorize(Roles = \"Admin\")].");

        var attr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)attrs[0];
        Assert.That(attr.Roles, Is.EqualTo("Admin"));
    }

    // ── OnGetAsync ────────────────────────────────────────────

    [Test]
    public async Task OnGetAsync_CompletesWithoutCallingService()
    {
        await _model.OnGetAsync(CancellationToken.None);

        _serviceMock.VerifyNoOtherCalls();
    }

    // ── Range values ──────────────────────────────────────────

    [TestCase(ReportRange.Last7Days)]
    [TestCase(ReportRange.Last30Days)]
    [TestCase(ReportRange.AllTime)]
    public async Task OnGetDashboardAsync_AllRanges_CallsServiceWithCorrectRange(ReportRange range)
    {
        var dto = BuildMinimalDashboard(range, ReportRoleFilter.All);
        _serviceMock.Setup(s => s.GetDashboardAsync(range, ReportRoleFilter.All, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(range, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.InstanceOf<AdminReportDashboardDto>());
        var returned = (AdminReportDashboardDto)json.Value!;
        Assert.That(returned.Range, Is.EqualTo(range));
        _serviceMock.Verify(s => s.GetDashboardAsync(range, ReportRoleFilter.All, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Role filter ───────────────────────────────────────────

    [TestCase(ReportRoleFilter.All)]
    [TestCase(ReportRoleFilter.Student)]
    [TestCase(ReportRoleFilter.Lecturer)]
    [TestCase(ReportRoleFilter.Admin)]
    public async Task OnGetDashboardAsync_AllRoles_PassedToService(ReportRoleFilter role)
    {
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, role);
        _serviceMock.Setup(s => s.GetDashboardAsync(ReportRange.Last7Days, role, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, role, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.Not.Null);
        _serviceMock.Verify(s => s.GetDashboardAsync(ReportRange.Last7Days, role, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Trend-subject behavior ────────────────────────────────

    [Test]
    public async Task OnGetDashboardAsync_NullTrendSubjectId_PassedAsNullToService()
    {
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All);
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        _serviceMock.Verify(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Is.InstanceOf<JsonResult>());
    }

    [Test]
    public async Task OnGetDashboardAsync_WithTrendSubjectId_PassedToService()
    {
        const int subjectId = 3;
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All, trendSubjectId: subjectId);
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), subjectId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, subjectId, CancellationToken.None);

        _serviceMock.Verify(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), subjectId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.Not.Null);
    }

    // ── Invalid enum inputs ───────────────────────────────────

    [Test]
    public async Task OnGetDashboardAsync_InvalidRange_ReturnsBadRequest()
    {
        var result = await _model.OnGetDashboardAsync((ReportRange)999, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _serviceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OnGetDashboardAsync_InvalidRole_ReturnsBadRequest()
    {
        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, (ReportRoleFilter)999, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _serviceMock.VerifyNoOtherCalls();
    }

    // ── Missing/partial/full token coverage ───────────────────

    [Test]
    public async Task OnGetDashboardAsync_AllTokensNull_ReturnsNullTokenFields()
    {
        var tokenMeasurement = new TokenMeasurementDto
        {
            MeasuredPromptTokens = null,
            MeasuredCompletionTokens = null,
            MeasuredTotalTokens = null,
            MeasuredMessageCount = 0,
            EligibleMessageCount = 10,
            CoveragePercent = 0.0,
        };
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All,
            tokenMeasurement: tokenMeasurement);
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var json = (JsonResult)result;
        Assert.That(json.Value, Is.InstanceOf<AdminReportDashboardDto>());
        var returned = (AdminReportDashboardDto)json.Value!;
        Assert.That(returned.Kpis.TokenMeasurement.MeasuredPromptTokens, Is.Null,
            "Missing token measurement must be null, never fabricated as zero.");
        Assert.That(returned.Kpis.TokenMeasurement.CoveragePercent, Is.EqualTo(0.0));
    }

    [Test]
    public async Task OnGetDashboardAsync_NoEligibleMessages_Returns100PercentCoverage()
    {
        var tokenMeasurement = new TokenMeasurementDto
        {
            MeasuredPromptTokens = 0,
            MeasuredCompletionTokens = 0,
            MeasuredTotalTokens = 0,
            MeasuredMessageCount = 0,
            EligibleMessageCount = 0,
            CoveragePercent = 100.0,
        };
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All,
            tokenMeasurement: tokenMeasurement);
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (AdminReportDashboardDto)((JsonResult)result).Value!;
        Assert.That(returned.Kpis.TokenMeasurement.CoveragePercent, Is.EqualTo(100.0));
    }

    [Test]
    public async Task OnGetDashboardAsync_PartialCoverage_ReturnsSumOfMeasured()
    {
        var tokenMeasurement = new TokenMeasurementDto
        {
            MeasuredPromptTokens = 5000,
            MeasuredCompletionTokens = 800,
            MeasuredTotalTokens = 5800,
            MeasuredMessageCount = 50,
            EligibleMessageCount = 100,
            CoveragePercent = 50.0,
        };
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All,
            tokenMeasurement: tokenMeasurement);
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (AdminReportDashboardDto)((JsonResult)result).Value!;
        Assert.That(returned.Kpis.TokenMeasurement.MeasuredPromptTokens, Is.EqualTo(5000L));
        Assert.That(returned.Kpis.TokenMeasurement.CoveragePercent, Is.EqualTo(50.0));
    }

    // ── Null subject bucket ───────────────────────────────────

    [Test]
    public async Task OnGetDashboardAsync_NullSubjectBucket_IncludedInSubjectUsage()
    {
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All);
        var subjectUsageWithNull = dto.SubjectUsage.ToList();
        subjectUsageWithNull.Add(new SubjectUsageDto
        {
            SubjectId = null,
            SubjectCode = null,
            SubjectName = "All subjects",
            UniqueActiveUserCount = 5,
            ActiveSessionCount = 6,
            CompletedAssistantGenerationCount = 10,
            TokenMeasurement = BuildZeroTokenMeasurement(),
        });
        var dtoWithNull = dto with { SubjectUsage = subjectUsageWithNull.AsReadOnly() };

        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dtoWithNull);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (AdminReportDashboardDto)((JsonResult)result).Value!;
        var nullBucket = returned.SubjectUsage.FirstOrDefault(s => s.SubjectId is null);
        Assert.That(nullBucket, Is.Not.Null, "Null-subject bucket must be preserved in SubjectUsage.");
        Assert.That(nullBucket!.SubjectName, Is.EqualTo("All subjects"));
    }

    // ── Zero-filled days ──────────────────────────────────────

    [Test]
    public async Task OnGetDashboardAsync_ZeroFilledDays_IncludedInGenerations()
    {
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All);
        var withZeroDay = dto with
        {
            AssistantGenerations = new List<DailyAssistantGenerationDto>
            {
                new() { Date = new DateOnly(2026, 7, 7), CompletedAssistantGenerationCount = 0 },
                new() { Date = new DateOnly(2026, 7, 8), CompletedAssistantGenerationCount = 10 },
            }.AsReadOnly(),
        };
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(withZeroDay);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (AdminReportDashboardDto)((JsonResult)result).Value!;
        Assert.That(returned.AssistantGenerations.Count, Is.EqualTo(2));
        Assert.That(returned.AssistantGenerations[0].CompletedAssistantGenerationCount, Is.EqualTo(0));
    }

    // ── Hot documents ─────────────────────────────────────────

    [Test]
    public async Task OnGetDashboardAsync_HotDocuments_EachDocumentAppearsOnce()
    {
        var docId = Guid.NewGuid();
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All);
        var withHotDoc = dto with
        {
            HotDocuments = new List<HotDocumentDto>
            {
                new() { DocumentId = docId, DocumentTitle = "Test Doc", SubjectId = 3, SubjectCode = "DB201", CitedAnswerCount = 42 },
            }.AsReadOnly(),
        };
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(withHotDoc);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (AdminReportDashboardDto)((JsonResult)result).Value!;
        var docIds = returned.HotDocuments.Select(d => d.DocumentId).ToList();
        Assert.That(docIds.Distinct().Count(), Is.EqualTo(docIds.Count), "Each document must appear at most once in hot documents.");
        Assert.That(returned.HotDocuments[0].CitedAnswerCount, Is.EqualTo(42));
    }

    // ── Null KPI values ───────────────────────────────────────

    [Test]
    public async Task OnGetDashboardAsync_NullSuccessRate_WhenNoTerminalGenerations()
    {
        var dto = BuildMinimalDashboard(ReportRange.Last7Days, ReportRoleFilter.All,
            successRate: null, citationCoverage: null, p95: null);
        _serviceMock.Setup(s => s.GetDashboardAsync(It.IsAny<ReportRange>(), It.IsAny<ReportRoleFilter>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _model.OnGetDashboardAsync(ReportRange.Last7Days, ReportRoleFilter.All, null, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<JsonResult>());
        var returned = (AdminReportDashboardDto)((JsonResult)result).Value!;
        Assert.That(returned.Kpis.GenerationSuccessRatePercent, Is.Null);
        Assert.That(returned.Kpis.CitationCoveragePercent, Is.Null);
        Assert.That(returned.Kpis.P95TotalResponseTimeMs, Is.Null);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static AdminReportDashboardDto BuildMinimalDashboard(
        ReportRange range,
        ReportRoleFilter role,
        int? trendSubjectId = null,
        TokenMeasurementDto? tokenMeasurement = null,
        double? successRate = 98.0,
        double? citationCoverage = 80.0,
        long? p95 = 2000)
    {
        var tm = tokenMeasurement ?? BuildFullTokenMeasurement();
        return new AdminReportDashboardDto
        {
            Range = range,
            Role = role,
            TrendSubjectId = trendSubjectId,
            FromUtc = DateTime.UtcNow.AddDays(-7),
            ToUtc = DateTime.UtcNow,
            TrendSubjectOptions = new List<ReportSubjectOptionDto>
            {
                new() { SubjectId = 3, SubjectCode = "DB201", SubjectName = "Database Systems" },
            }.AsReadOnly(),
            Kpis = new AdminReportKpiDto
            {
                UniqueActiveUserCount = 10,
                ActiveSessionCount = 12,
                CompletedAssistantGenerationCount = 50,
                TerminalGenerationCount = 51,
                GenerationSuccessRatePercent = successRate,
                CitationCoveragePercent = citationCoverage,
                P95TotalResponseTimeMs = p95,
                TokenMeasurement = tm,
            },
            AssistantGenerations = new List<DailyAssistantGenerationDto>().AsReadOnly(),
            TokenUsage = new List<DailyTokenUsageDto>().AsReadOnly(),
            ResponseLatency = new List<DailyResponseLatencyDto>().AsReadOnly(),
            SubjectUsage = new List<SubjectUsageDto>().AsReadOnly(),
            SubjectsNeedingAttention = new List<SubjectHealthDto>().AsReadOnly(),
            IndexingStatus = new IndexingStatusSummaryDto
            {
                IndexedDocumentCount = 10,
                ProcessingDocumentCount = 1,
                FailedDocumentCount = 0,
            },
            HotDocuments = new List<HotDocumentDto>().AsReadOnly(),
        };
    }

    private static TokenMeasurementDto BuildFullTokenMeasurement() => new()
    {
        MeasuredPromptTokens = 10000,
        MeasuredCompletionTokens = 1500,
        MeasuredTotalTokens = 11500,
        MeasuredMessageCount = 48,
        EligibleMessageCount = 50,
        CoveragePercent = 96.0,
    };

    private static TokenMeasurementDto BuildZeroTokenMeasurement() => new()
    {
        MeasuredPromptTokens = 0,
        MeasuredCompletionTokens = 0,
        MeasuredTotalTokens = 0,
        MeasuredMessageCount = 0,
        EligibleMessageCount = 0,
        CoveragePercent = 100.0,
    };
}
