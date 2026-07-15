using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Contracts.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace UnitTests;

[TestFixture, NonParallelizable]
public sealed class AdminReportRepositoryIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private const int Db201Id = 910001;
    private const int Db202Id = 910002;
    private string _connectionString = null!;
    private EduChatAiDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString)) Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");

        _context = CreateContext();
        _transaction = await _context.Database.BeginTransactionAsync();
        await SeedDashboardScenarioAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_transaction is not null) await _transaction.RollbackAsync();
        if (_context is not null) await _context.DisposeAsync();
    }

    [Test]
    public async Task SubjectAndEarliestQueries_UseExistenceAndRoleFilteredChatActivity()
    {
        var repository = new AdminReportRepository(_context);

        var exists = await repository.SubjectExistsAsync(Db201Id);
        var missing = await repository.SubjectExistsAsync(int.MaxValue);
        var studentEarliest = await repository.GetEarliestChatEventUtcAsync(ReportRoleFilter.Student);
        var lecturerEarliest = await repository.GetEarliestChatEventUtcAsync(ReportRoleFilter.Lecturer);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exists, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(studentEarliest, Is.EqualTo(Utc(2026, 7, 8, 16, 58)));
            Assert.That(lecturerEarliest, Is.EqualTo(Utc(2026, 7, 12, 10, 0)));
        }
    }

    [Test]
    public async Task Dashboard_UsesRoleForGlobalDataAndSubjectOnlyForTrends()
    {
        var repository = new AdminReportRepository(_context);

        var data = await repository.GetDashboardDataAsync(new AdminReportQuery(
            Utc(2026, 7, 8, 17, 0), Utc(2026, 7, 15, 17, 0), ReportRoleFilter.Student, Db201Id));

        var db201 = data.SubjectUsage.Single(e => e.SubjectId == Db201Id);
        var db202 = data.SubjectUsage.Single(e => e.SubjectId == Db202Id);
        var flexible = data.SubjectUsage.Single(e => e.SubjectId is null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.Kpis.UniqueActiveUserCount, Is.EqualTo(1));
            Assert.That(data.Kpis.ActiveSessionCount, Is.EqualTo(3));
            Assert.That(data.Kpis.CompletedAssistantGenerationCount, Is.EqualTo(4));
            Assert.That(data.Kpis.TerminalGenerationCount, Is.EqualTo(5));
            Assert.That(data.Kpis.GenerationSuccessRatePercent, Is.EqualTo(80));
            Assert.That(data.Kpis.CitationCoveragePercent, Is.EqualTo(200d / 3).Within(0.0001));
            Assert.That(data.Kpis.P95TotalResponseTimeMs, Is.EqualTo(300));
            AssertToken(data.Kpis.TokenMeasurement, 35, 20, 55, 3, 4, 75);

            Assert.That(data.AssistantGenerations, Is.EqualTo(new[]
            {
                new DailyAssistantGenerationDto { Date = new DateOnly(2026, 7, 9), CompletedAssistantGenerationCount = 1 },
                new DailyAssistantGenerationDto { Date = new DateOnly(2026, 7, 10), CompletedAssistantGenerationCount = 1 },
            }));
            Assert.That(data.TokenUsage, Has.Count.EqualTo(2));
            AssertToken(data.TokenUsage[0].TokenMeasurement, 10, 5, 15, 1, 1, 100);
            AssertToken(data.TokenUsage[1].TokenMeasurement, 20, 10, 30, 1, 1, 100);
            Assert.That(data.ResponseLatency.Select(e => e.P95TotalResponseTimeMs), Is.EqualTo(new long?[] { 100, 200 }));

            Assert.That(db201.CompletedAssistantGenerationCount, Is.EqualTo(2));
            AssertToken(db201.TokenMeasurement, 30, 15, 45, 2, 2, 100);
            Assert.That(db202.CompletedAssistantGenerationCount, Is.EqualTo(1));
            AssertToken(db202.TokenMeasurement, null, null, null, 0, 1, 0);
            Assert.That(flexible.SubjectName, Is.EqualTo("Flexible subjects"));
            Assert.That(flexible.CompletedAssistantGenerationCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Dashboard_CountsDistinctCitedAnswersAndCurrentDocumentStatuses()
    {
        var repository = new AdminReportRepository(_context);

        var data = await repository.GetDashboardDataAsync(new AdminReportQuery(
            Utc(2026, 7, 8, 17, 0), Utc(2026, 7, 15, 17, 0), ReportRoleFilter.Student, null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.HotDocuments.Select(e => (e.DocumentTitle, e.CitedAnswerCount)), Is.EqualTo(new[]
            {
                ("Alpha", 2),
                ("Beta", 1),
                ("Gamma", 1),
            }));
            Assert.That(data.IndexingStatus, Is.EqualTo(new IndexingStatusSummaryDto
            {
                IndexedDocumentCount = 1,
                ProcessingDocumentCount = 2,
                FailedDocumentCount = 1,
            }));
            Assert.That(data.TrendSubjectOptions.Select(e => e.SubjectCode), Is.EqualTo(new[] { "DB201", "DB202" }));
        }
    }

    [Test]
    public async Task Dashboard_CompletedGenerationWithoutMetricsRemainsUnmeasured()
    {
        var repository = new AdminReportRepository(_context);

        var data = await repository.GetDashboardDataAsync(new AdminReportQuery(
            Utc(2026, 7, 8, 17, 0), Utc(2026, 7, 15, 17, 0), ReportRoleFilter.Lecturer, null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.Kpis.CompletedAssistantGenerationCount, Is.EqualTo(1));
            Assert.That(data.Kpis.P95TotalResponseTimeMs, Is.Null);
            AssertToken(data.Kpis.TokenMeasurement, null, null, null, 0, 1, 0);
            Assert.That(data.ResponseLatency.Single().P95TotalResponseTimeMs, Is.Null);
            AssertToken(data.TokenUsage.Single().TokenMeasurement, null, null, null, 0, 1, 0);
        }
    }

    [Test]
    public async Task Dashboard_NoEligibleActivityReportsZeroMeasuredUsageWithFullCoverage()
    {
        var repository = new AdminReportRepository(_context);

        var data = await repository.GetDashboardDataAsync(new AdminReportQuery(
            Utc(2026, 8, 1, 17, 0), Utc(2026, 8, 2, 17, 0), ReportRoleFilter.Student, null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.Kpis.GenerationSuccessRatePercent, Is.Null);
            Assert.That(data.Kpis.CitationCoveragePercent, Is.Null);
            Assert.That(data.Kpis.P95TotalResponseTimeMs, Is.Null);
            AssertToken(data.Kpis.TokenMeasurement, 0, 0, 0, 0, 0, 100);
            Assert.That(data.AssistantGenerations, Is.Empty);
            Assert.That(data.TokenUsage, Is.Empty);
            Assert.That(data.ResponseLatency, Is.Empty);
        }
    }

    [Test]
    public async Task Dashboard_HealthRequiresTenCompletionsAndUsesNearestRankP95()
    {
        await SeedHealthSubjectAsync();
        await SeedAdditionalDb201HealthAsync();
        var repository = new AdminReportRepository(_context);

        var data = await repository.GetDashboardDataAsync(new AdminReportQuery(
            Utc(2026, 7, 8, 17, 0), Utc(2026, 7, 15, 17, 0), ReportRoleFilter.Student, null));

        var db203 = data.SubjectsNeedingAttention[0];
        var db201 = data.SubjectsNeedingAttention[1];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.SubjectsNeedingAttention.Select(e => e.SubjectCode), Is.EqualTo(new[] { "DB203", "DB201" }));
            Assert.That(db203.CompletedAssistantGenerationCount, Is.EqualTo(10));
            Assert.That(db203.NoContextRatePercent, Is.EqualTo(50));
            Assert.That(db203.GenerationFailureRatePercent, Is.EqualTo(100d / 6).Within(0.0001));
            Assert.That(db203.P95TotalResponseTimeMs, Is.EqualTo(100));
            Assert.That(db201.CompletedAssistantGenerationCount, Is.EqualTo(10));
            Assert.That(db201.NoContextRatePercent, Is.EqualTo(40));
            Assert.That(db201.GenerationFailureRatePercent, Is.EqualTo(100d / 6).Within(0.0001));
            Assert.That(db201.P95TotalResponseTimeMs, Is.EqualTo(200));
        }
    }

    private async Task SeedDashboardScenarioAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO roles (id, name, normalized_name) VALUES
                ('10000000-0000-0000-0000-000000000001', 'Student', 'STUDENT'),
                ('10000000-0000-0000-0000-000000000002', 'Lecturer', 'LECTURER');
            INSERT INTO users (id, full_name, is_active, email_confirmed, phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count) VALUES
                ('20000000-0000-0000-0000-000000000001', 'Report Student', TRUE, TRUE, FALSE, FALSE, FALSE, 0),
                ('20000000-0000-0000-0000-000000000002', 'Report Lecturer', TRUE, TRUE, FALSE, FALSE, FALSE, 0);
            INSERT INTO user_roles (user_id, role_id) VALUES
                ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001'),
                ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002');
            INSERT INTO subjects (id, code, name) VALUES
                (910001, 'DB201', 'Database Systems'),
                (910002, 'DB202', 'Advanced Databases');
            INSERT INTO chat_sessions (id, user_id, subject_id, title) VALUES
                ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 910001, 'Student DB201'),
                ('30000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 910002, 'Student DB202'),
                ('30000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000001', NULL, 'Student general'),
                ('30000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000002', 910001, 'Lecturer DB201');
            INSERT INTO documents (id, subject_id, uploader_id, title, original_file_name, file_type, status, uploaded_at) VALUES
                ('40000000-0000-0000-0000-000000000001', 910001, '20000000-0000-0000-0000-000000000001', 'Alpha', 'alpha.pdf', 2, 7, '2026-07-01T00:00:00Z'),
                ('40000000-0000-0000-0000-000000000002', 910001, '20000000-0000-0000-0000-000000000001', 'Beta', 'beta.pdf', 2, 0, '2026-07-01T00:00:00Z'),
                ('40000000-0000-0000-0000-000000000003', 910002, '20000000-0000-0000-0000-000000000001', 'Gamma', 'gamma.pdf', 2, -1, '2026-07-01T00:00:00Z'),
                ('40000000-0000-0000-0000-000000000004', 910002, '20000000-0000-0000-0000-000000000001', 'Delta', 'delta.pdf', 2, 6, '2026-07-01T00:00:00Z');
            INSERT INTO chunks (id, document_id, chunk_index, chunk_text, chunk_strategy) VALUES
                ('50000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 0, 'Alpha one', 'FixedLength'),
                ('50000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000001', 1, 'Alpha two', 'FixedLength'),
                ('50000000-0000-0000-0000-000000000003', '40000000-0000-0000-0000-000000000002', 0, 'Beta one', 'FixedLength'),
                ('50000000-0000-0000-0000-000000000004', '40000000-0000-0000-0000-000000000003', 0, 'Gamma one', 'FixedLength');

            INSERT INTO chat_messages (id, chat_session_id, content, raw_content, chat_role, sent_at, status, message_index, is_selected_variant) VALUES
                ('60000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'q1', 'q1', 1, '2026-07-09T16:58:00Z', 2, 1, FALSE),
                ('60000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'q2', 'q2', 1, '2026-07-10T10:00:00Z', 2, 3, FALSE),
                ('60000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000002', 'q3', 'q3', 1, '2026-07-11T10:00:00Z', 2, 1, FALSE),
                ('60000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000003', 'q4', 'q4', 1, '2026-07-12T09:00:00Z', 2, 1, FALSE),
                ('60000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000004', 'q5', 'q5', 1, '2026-07-12T10:00:00Z', 2, 1, FALSE),
                ('60000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000001', 'end', 'end', 1, '2026-07-15T17:00:00Z', 2, 5, FALSE),
                ('60000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000001', 'before', 'before', 1, '2026-07-08T16:58:00Z', 2, 7, FALSE);
            INSERT INTO chat_messages (id, chat_session_id, content, raw_content, chat_role, sent_at, status, message_index, in_reply_to_message_id, variant_index, is_selected_variant) VALUES
                ('70000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'a1', 'a1', 2, '2026-07-09T16:59:00Z', 2, 2, '60000000-0000-0000-0000-000000000001', 1, TRUE),
                ('70000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'a1v2', 'a1v2', 2, '2026-07-09T17:00:00Z', 2, 2, '60000000-0000-0000-0000-000000000001', 2, FALSE),
                ('70000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000001', '', '', 2, '2026-07-10T10:01:00Z', 3, 4, '60000000-0000-0000-0000-000000000002', 1, TRUE),
                ('70000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000002', 'a3', 'a3', 2, '2026-07-11T10:01:00Z', 2, 2, '60000000-0000-0000-0000-000000000003', 1, TRUE),
                ('70000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000003', 'a4', 'a4', 2, '2026-07-12T09:01:00Z', 2, 2, '60000000-0000-0000-0000-000000000004', 1, TRUE),
                ('70000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000004', 'a5', 'a5', 2, '2026-07-12T10:01:00Z', 2, 2, '60000000-0000-0000-0000-000000000005', 1, TRUE),
                ('70000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000001', 'outside', 'outside', 2, '2026-07-15T17:00:01Z', 2, 6, '60000000-0000-0000-0000-000000000006', 1, TRUE),
                ('70000000-0000-0000-0000-000000000008', '30000000-0000-0000-0000-000000000001', 'before', 'before', 2, '2026-07-08T16:59:00Z', 2, 8, '60000000-0000-0000-0000-000000000007', 1, TRUE);
            INSERT INTO chat_message_generation_metrics (id, retrieved_chunk_count, context_chunk_count, prompt_tokens, completion_tokens, retrieval_time_ms, total_response_time_ms) VALUES
                ('70000000-0000-0000-0000-000000000001', 2, 0, 10, 5, 10, 100),
                ('70000000-0000-0000-0000-000000000002', 2, 1, 20, 10, 10, 200),
                ('70000000-0000-0000-0000-000000000004', 1, 1, 99, NULL, 10, 300),
                ('70000000-0000-0000-0000-000000000005', 1, 1, 5, 5, 10, 50),
                ('70000000-0000-0000-0000-000000000007', 1, 1, 999, 999, 10, 999),
                ('70000000-0000-0000-0000-000000000008', 1, 1, 999, 999, 10, 999);
            INSERT INTO citations (id, chat_message_id, chunk_id, citation_index, similarity_score) VALUES
                ('80000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 1, 0.9),
                ('80000000-0000-0000-0000-000000000002', '70000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000002', 2, 0.8),
                ('80000000-0000-0000-0000-000000000003', '70000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000001', 1, 0.9),
                ('80000000-0000-0000-0000-000000000004', '70000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000003', 1, 0.9),
                ('80000000-0000-0000-0000-000000000005', '70000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000004', 3, 0.7),
                ('80000000-0000-0000-0000-000000000006', '70000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000003', 1, 0.9);
            """);
    }

    private async Task SeedHealthSubjectAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO subjects (id, code, name) VALUES (910003, 'DB203', 'Database Reliability');
            INSERT INTO chat_sessions (id, user_id, subject_id, title) VALUES
                ('30000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000001', 910003, 'Student DB203');
            """);

        for (var index = 0; index < 12; index++)
        {
            var userId = Guid.NewGuid();
            var assistantId = Guid.NewGuid();
            var sentAt = Utc(2026, 7, 13, index, 0);
            var userIndex = index * 2 + 1;
            var status = index < 10 ? 2 : 3;
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO chat_messages (id, chat_session_id, content, raw_content, chat_role, sent_at, status, message_index, is_selected_variant)
                VALUES ({userId}, '30000000-0000-0000-0000-000000000005', 'health-q', 'health-q', 1, {sentAt}, 2, {userIndex}, FALSE);
                INSERT INTO chat_messages (id, chat_session_id, content, raw_content, chat_role, sent_at, status, message_index, in_reply_to_message_id, variant_index, is_selected_variant)
                VALUES ({assistantId}, '30000000-0000-0000-0000-000000000005', 'health-a', 'health-a', 2, {sentAt.AddMinutes(1)}, {status}, {userIndex + 1}, {userId}, 1, TRUE);
                """);
            if (status == 2)
            {
                var contextCount = index < 5 ? 0 : 1;
                var totalMs = (index + 1) * 10;
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO chat_message_generation_metrics (id, retrieved_chunk_count, context_chunk_count, prompt_tokens, completion_tokens, retrieval_time_ms, total_response_time_ms)
                    VALUES ({assistantId}, 1, {contextCount}, 1, 1, 1, {totalMs});
                    """);
            }
        }
    }

    private async Task SeedAdditionalDb201HealthAsync()
    {
        for (var index = 0; index < 9; index++)
        {
            var userId = Guid.NewGuid();
            var assistantId = Guid.NewGuid();
            var sentAt = Utc(2026, 7, 14, index, 0);
            var userIndex = index * 2 + 9;
            var status = index < 8 ? 2 : 3;
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO chat_messages (id, chat_session_id, content, raw_content, chat_role, sent_at, status, message_index, is_selected_variant)
                VALUES ({userId}, '30000000-0000-0000-0000-000000000001', 'db201-health-q', 'db201-health-q', 1, {sentAt}, 2, {userIndex}, FALSE);
                INSERT INTO chat_messages (id, chat_session_id, content, raw_content, chat_role, sent_at, status, message_index, in_reply_to_message_id, variant_index, is_selected_variant)
                VALUES ({assistantId}, '30000000-0000-0000-0000-000000000001', 'db201-health-a', 'db201-health-a', 2, {sentAt.AddMinutes(1)}, {status}, {userIndex + 1}, {userId}, 1, TRUE);
                """);
            if (status == 2)
            {
                var contextCount = index < 3 ? 0 : 1;
                var totalMs = (index + 1) * 10;
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO chat_message_generation_metrics (id, retrieved_chunk_count, context_chunk_count, prompt_tokens, completion_tokens, retrieval_time_ms, total_response_time_ms)
                    VALUES ({assistantId}, 1, {contextCount}, 1, 1, 1, {totalMs});
                    """);
            }
        }
    }

    private EduChatAiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new EduChatAiDbContext(options);
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute) => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static void AssertToken(TokenMeasurementDto actual, long? prompt, long? completion, long? total, int measured, int eligible, double coverage)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.MeasuredPromptTokens, Is.EqualTo(prompt));
            Assert.That(actual.MeasuredCompletionTokens, Is.EqualTo(completion));
            Assert.That(actual.MeasuredTotalTokens, Is.EqualTo(total));
            Assert.That(actual.MeasuredMessageCount, Is.EqualTo(measured));
            Assert.That(actual.EligibleMessageCount, Is.EqualTo(eligible));
            Assert.That(actual.CoveragePercent, Is.EqualTo(coverage).Within(0.0001));
        }
    }
}
