using DataAccess.Data;
using DataAccess.Seeding;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace UnitTests;

[TestFixture, NonParallelizable]
public sealed class AdminReportDemoDataSeederIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private const string DemoEmailSuffix = "@admin-reports-demo.invalid";
    private EduChatAiDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;

    [SetUp]
    public async Task SetUp()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");

        _context = new EduChatAiDbContext(new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options);
        _transaction = await _context.Database.BeginTransactionAsync();
        await EnsurePrerequisitesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_transaction is not null) await _transaction.RollbackAsync();
        if (_context is not null) await _context.DisposeAsync();
    }

    [Test]
    public async Task SeedAsync_CreatesRepeatableSixtyDayDashboardHistoryWithoutTouchingOtherRows()
    {
        var sentinel = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin-report-sentinel",
            NormalizedUserName = "ADMIN-REPORT-SENTINEL",
            Email = "admin-report-sentinel@example.invalid",
            NormalizedEmail = "ADMIN-REPORT-SENTINEL@EXAMPLE.INVALID",
            FullName = "Non-demo sentinel",
            IsActive = true,
        };
        _context.Users.Add(sentinel);
        await _context.SaveChangesAsync();

        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        var seeder = new AdminReportDemoDataSeeder(_context, clock);

        await seeder.SeedAsync();
        var first = await ReadSnapshotAsync();
        await seeder.SeedAsync();
        var second = await ReadSnapshotAsync();
        var nextDaySeeder = new AdminReportDemoDataSeeder(
            _context,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero)));
        await nextDaySeeder.SeedAsync();
        var nextDay = await ReadSnapshotAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.UserCount, Is.EqualTo(12));
            Assert.That(first.RoleAssignmentCount, Is.EqualTo(12));
            Assert.That(first.UsersWithWrongRoleCount, Is.Zero);
            Assert.That(first.LoginableUserCount, Is.Zero);
            Assert.That(first.SessionCount, Is.EqualTo(36));
            Assert.That(first.UsersWithoutThreeSessions, Is.Zero);
            Assert.That(first.InvalidMembershipSessionCount, Is.Zero);
            Assert.That(first.UserTurnCount, Is.EqualTo(180));
            Assert.That(first.TerminalVariantCount, Is.EqualTo(200));
            Assert.That(first.CompletedCount, Is.EqualTo(186));
            Assert.That(first.FailedCount, Is.EqualTo(14));
            Assert.That(first.SelectedVariantCount, Is.EqualTo(180));
            Assert.That(first.TurnsWithoutExactlyOneSelectedVariant, Is.Zero);
            Assert.That(first.InvalidMessageShapeCount, Is.Zero);
            Assert.That(first.ExtraVariantCount, Is.EqualTo(20));
            Assert.That(first.MeasuredCompletionCount, Is.EqualTo(153));
            Assert.That(first.PartialTokenCount, Is.EqualTo(10));
            Assert.That(first.AbsentTokenCount, Is.EqualTo(23));
            Assert.That(first.CitedCompletionCount, Is.EqualTo(148));
            Assert.That(first.NoContextCompletionCount, Is.EqualTo(23));
            Assert.That(first.ContextCountMismatchCount, Is.Zero);
            Assert.That(first.InvalidContextOrderCount, Is.Zero);
            Assert.That(first.CitationsFromNonIndexedDocuments, Is.Zero);
            Assert.That(first.DocumentCount, Is.EqualTo(12));
            Assert.That(first.IndexedDocumentCount, Is.EqualTo(7));
            Assert.That(first.ProcessingDocumentCount, Is.EqualTo(3));
            Assert.That(first.FailedDocumentCount, Is.EqualTo(2));
            Assert.That(first.ActiveDayCount, Is.EqualTo(52));
            Assert.That(first.HistoryDaySpan, Is.EqualTo(60));
            Assert.That(first.SessionCountsBySubject, Is.EqualTo(new Dictionary<string, int>
            {
                ["DB201"] = 20,
                ["AI301"] = 8,
                ["SE401"] = 4,
                ["Flexible subjects"] = 4,
            }));
            Assert.That(first.TerminalCountsBySubject, Is.EqualTo(new Dictionary<string, int>
            {
                ["DB201"] = 112,
                ["AI301"] = 44,
                ["SE401"] = 22,
                ["Flexible subjects"] = 22,
            }));
            Assert.That(first.CompletedCountsBySubject, Is.EqualTo(new Dictionary<string, int>
            {
                ["DB201"] = 108,
                ["AI301"] = 39,
                ["SE401"] = 18,
                ["Flexible subjects"] = 21,
            }));
            Assert.That(first.P95BySubject, Is.EqualTo(new Dictionary<string, long>
            {
                ["DB201"] = 1_800,
                ["AI301"] = 2_400,
                ["SE401"] = 3_200,
            }));
            Assert.That(second.DeterministicFingerprint, Is.EqualTo(first.DeterministicFingerprint));
            Assert.That(nextDay.FirstActivityDate, Is.EqualTo(first.FirstActivityDate.AddDays(1)));
            Assert.That(nextDay.UserCount, Is.EqualTo(first.UserCount));
            Assert.That(nextDay.SessionCount, Is.EqualTo(first.SessionCount));
            Assert.That(nextDay.TerminalVariantCount, Is.EqualTo(first.TerminalVariantCount));
            Assert.That(nextDay.ActiveDayCount, Is.EqualTo(first.ActiveDayCount));
            Assert.That(nextDay.HistoryDaySpan, Is.EqualTo(first.HistoryDaySpan));
            Assert.That(nextDay.DeterministicFingerprint, Is.Not.EqualTo(first.DeterministicFingerprint));
            Assert.That(await _context.Users.AnyAsync(user => user.Id == sentinel.Id), Is.True);
        }
    }

    [Test]
    public async Task SeedAsync_PreservesNonDemoChatCitationToDemoChunk()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        var seeder = new AdminReportDemoDataSeeder(_context, clock);
        await seeder.SeedAsync();

        var subject = await _context.Subjects.SingleAsync(entity => entity.Code == "DB201");
        var studentRole = await _context.Roles.SingleAsync(entity => entity.Name == "Student");
        var chunk = await _context.Chunks
            .Where(entity => entity.Document.Title.StartsWith(AdminReportDemoDataSeeder.DemoDocumentTitlePrefix)
                && entity.Document.Status == DocumentStatus.Indexed)
            .OrderBy(entity => entity.Id)
            .FirstAsync();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "demo-source-citation-sentinel",
            NormalizedUserName = "DEMO-SOURCE-CITATION-SENTINEL",
            Email = "demo-source-citation-sentinel@example.invalid",
            NormalizedEmail = "DEMO-SOURCE-CITATION-SENTINEL@EXAMPLE.INVALID",
            FullName = "Non-demo citation sentinel",
            IsActive = true,
        };
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SubjectId = subject.Id,
            Title = "Non-demo chat using a demo source",
        };
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            ChatRole = ChatRole.User,
            Content = "Explain the demo source.",
            RawContent = "Explain the demo source.",
            SentAt = clock.GetUtcNow().UtcDateTime,
            Status = MessageStatus.Completed,
            MessageIndex = 1,
        };
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            ChatRole = ChatRole.Assistant,
            Content = "Grounded answer.",
            RawContent = "Grounded answer.",
            SentAt = clock.GetUtcNow().UtcDateTime.AddSeconds(1),
            Status = MessageStatus.Completed,
            MessageIndex = 2,
            InReplyToMessageId = userMessage.Id,
            VariantIndex = 1,
            IsSelectedVariant = true,
        };
        var citation = new Citation
        {
            Id = Guid.NewGuid(),
            ChatMessageId = assistantMessage.Id,
            ChunkId = chunk.Id,
            CitationIndex = 1,
            SimilarityScore = 0.9,
        };
        _context.AddRange(
            user,
            new ApplicationUserRole { UserId = user.Id, RoleId = studentRole.Id },
            new Membership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SubjectId = subject.Id,
                Role = MembershipRole.Student,
                AssignedAt = clock.GetUtcNow().UtcDateTime,
            },
            session,
            userMessage,
            assistantMessage,
            citation);
        await _context.SaveChangesAsync();

        await seeder.SeedAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await _context.ChatMessages.AnyAsync(entity => entity.Id == assistantMessage.Id), Is.True);
            Assert.That(await _context.Citations.AnyAsync(entity => entity.Id == citation.Id && entity.ChunkId == chunk.Id), Is.True);
        }
    }

    [Test]
    public async Task SeedAsync_MissingRequiredSubjectFailsBeforeReplacingExistingDemoRows()
    {
        var seeder = new AdminReportDemoDataSeeder(
            _context,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero)));
        await seeder.SeedAsync();
        var before = await ReadSnapshotAsync();
        await _context.Subjects.Where(subject => subject.Code == "DB201")
            .ExecuteUpdateAsync(setters => setters.SetProperty(subject => subject.Code, "DB201-MISSING"));

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());

        await _context.Subjects.Where(subject => subject.Code == "DB201-MISSING")
            .ExecuteUpdateAsync(setters => setters.SetProperty(subject => subject.Code, "DB201"));
        var after = await ReadSnapshotAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(error!.Message, Does.Contain("DB201"));
            Assert.That(after.DeterministicFingerprint, Is.EqualTo(before.DeterministicFingerprint));
        }
    }

    private async Task<DemoSnapshot> ReadSnapshotAsync()
    {
        _context.ChangeTracker.Clear();
        var users = await _context.Users.AsNoTracking()
            .Where(user => user.Email != null && user.Email.EndsWith(DemoEmailSuffix))
            .ToListAsync();
        var userIds = users.Select(user => user.Id).ToArray();
        var roleCounts = await _context.UserRoles.AsNoTracking()
            .Where(userRole => userIds.Contains(userRole.UserId))
            .GroupBy(userRole => userRole.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.UserId, row => row.Count);
        var memberships = await _context.Memberships.AsNoTracking()
            .Where(membership => userIds.Contains(membership.UserId))
            .Select(membership => new { membership.UserId, membership.SubjectId })
            .ToListAsync();
        var membershipKeys = memberships.Select(membership => (membership.UserId, membership.SubjectId)).ToHashSet();
        var sessions = await _context.ChatSessions.AsNoTracking()
            .Where(session => userIds.Contains(session.UserId))
            .Include(session => session.User)
            .Include(session => session.Subject)
            .ToListAsync();
        var sessionIds = sessions.Select(session => session.Id).ToArray();
        var messages = await _context.ChatMessages.AsNoTracking()
            .Where(message => sessionIds.Contains(message.ChatSessionId))
            .Include(message => message.GenerationMetrics)
            .Include(message => message.RetrievedContexts)
            .Include(message => message.Citations)
            .ToListAsync();
        var assistant = messages.Where(message => message.ChatRole == ChatRole.Assistant).ToArray();
        var userMessages = messages.Where(message => message.ChatRole == ChatRole.User).ToArray();
        var completed = assistant.Where(message => message.Status == MessageStatus.Completed).ToArray();
        var documents = await _context.Documents.AsNoTracking()
            .Where(document => document.Title.StartsWith(AdminReportDemoDataSeeder.DemoDocumentTitlePrefix))
            .ToListAsync();
        var assistantIds = assistant.Select(message => message.Id).ToArray();
        var citationsFromNonIndexedDocuments = await _context.Citations.AsNoTracking()
            .CountAsync(citation => assistantIds.Contains(citation.ChatMessageId)
                && citation.Chunk.Document.Status != DocumentStatus.Indexed);
        var localDates = messages.Where(message => message.ChatRole == ChatRole.User)
            .Select(message => DateOnly.FromDateTime(message.SentAt.AddHours(7)))
            .Distinct()
            .Order()
            .ToArray();

        string Bucket(ChatSession session) => session.Subject?.Code ?? "Flexible subjects";
        var sessionById = sessions.ToDictionary(session => session.Id);
        var terminalBySubject = assistant.GroupBy(message => Bucket(sessionById[message.ChatSessionId]))
            .ToDictionary(group => group.Key, group => group.Count());
        var completedBySubject = completed.GroupBy(message => Bucket(sessionById[message.ChatSessionId]))
            .ToDictionary(group => group.Key, group => group.Count());
        var p95BySubject = completed.Where(message => sessionById[message.ChatSessionId].SubjectId.HasValue)
            .GroupBy(message => Bucket(sessionById[message.ChatSessionId]))
            .ToDictionary(group => group.Key, group => P95(group.Select(message => message.GenerationMetrics!.TotalResponseTimeMs)));
        var deterministicFingerprint = string.Join('|',
            users.OrderBy(user => user.Id).Select(user => $"u:{user.Id}:{roleCounts.GetValueOrDefault(user.Id)}")
                .Concat(sessions.OrderBy(session => session.Id).Select(session => $"s:{session.Id}:{session.SubjectId}"))
                .Concat(messages.OrderBy(message => message.Id).Select(message =>
                    $"m:{message.Id}:{message.SentAt.Ticks}:{message.Status}:{message.MessageIndex}:{message.VariantIndex}:{message.IsSelectedVariant}"))
                .Concat(documents.OrderBy(document => document.Id).Select(document =>
                    $"d:{document.Id}:{document.SubjectId}:{document.Status}:{document.UploadedAt.Ticks}")));

        return new DemoSnapshot(
            users.Count,
            roleCounts.Values.Sum(),
            userIds.Count(userId => roleCounts.GetValueOrDefault(userId) != 1),
            users.Count(user => user.PasswordHash != null || !user.Email!.EndsWith(DemoEmailSuffix)),
            sessions.Count,
            users.Count(user => sessions.Count(session => session.UserId == user.Id) != 3),
            sessions.Count(session =>
                !session.User.Email!.Contains("-admin-", StringComparison.Ordinal)
                && (session.SubjectId.HasValue
                    ? !membershipKeys.Contains((session.UserId, session.SubjectId.Value))
                    : !memberships.Any(membership => membership.UserId == session.UserId))),
            messages.Count(message => message.ChatRole == ChatRole.User),
            assistant.Length,
            completed.Length,
            assistant.Count(message => message.Status == MessageStatus.Failed),
            assistant.Count(message => message.IsSelectedVariant),
            messages.Where(message => message.ChatRole == ChatRole.User)
                .Count(userMessage => assistant.Count(candidate => candidate.InReplyToMessageId == userMessage.Id && candidate.IsSelectedVariant) != 1),
            userMessages.Count(message => message.MessageIndex is null || message.MessageIndex <= 0 || message.MessageIndex % 2 != 1)
                + assistant.Count(message => message.MessageIndex is null || message.MessageIndex <= 0 || message.MessageIndex % 2 != 0
                    || message.VariantIndex is null or <= 0
                    || userMessages.All(userMessage => userMessage.Id != message.InReplyToMessageId
                        || userMessage.MessageIndex + 1 != message.MessageIndex)),
            assistant.Length - messages.Count(message => message.ChatRole == ChatRole.User),
            completed.Count(message => message.GenerationMetrics?.PromptTokens.HasValue == true && message.GenerationMetrics.CompletionTokens.HasValue),
            completed.Count(message => message.GenerationMetrics is { } metrics && metrics.PromptTokens.HasValue != metrics.CompletionTokens.HasValue),
            completed.Count(message => message.GenerationMetrics is { PromptTokens: null, CompletionTokens: null }),
            completed.Count(message => message.Citations.Count > 0),
            completed.Count(message => message.GenerationMetrics?.ContextChunkCount == 0),
            completed.Count(message => message.GenerationMetrics is null || message.GenerationMetrics.ContextChunkCount != message.RetrievedContexts.Count),
            completed.Count(message => !message.RetrievedContexts.OrderBy(row => row.ContextIndex).Select(row => row.ContextIndex)
                .SequenceEqual(Enumerable.Range(0, message.RetrievedContexts.Count))),
            citationsFromNonIndexedDocuments,
            documents.Count,
            documents.Count(document => document.Status == DocumentStatus.Indexed),
            documents.Count(document => document.Status is not DocumentStatus.Indexed and not DocumentStatus.Failed),
            documents.Count(document => document.Status == DocumentStatus.Failed),
            localDates.Length,
            localDates[^1].DayNumber - localDates[0].DayNumber + 1,
            localDates[0],
            sessions.GroupBy(Bucket).ToDictionary(group => group.Key, group => group.Count()),
            terminalBySubject,
            completedBySubject,
            p95BySubject,
            deterministicFingerprint);
    }

    private async Task EnsurePrerequisitesAsync()
    {
        foreach (var roleName in new[] { "Student", "Lecturer", "Admin" })
        {
            if (!await _context.Roles.AnyAsync(role => role.Name == roleName))
            {
                _context.Roles.Add(new ApplicationRole(roleName)
                {
                    Id = Guid.NewGuid(),
                    NormalizedName = roleName.ToUpperInvariant(),
                });
            }
        }

        foreach (var (code, name) in new[]
        {
            ("DB201", "Database Systems"),
            ("AI301", "Artificial Intelligence"),
            ("SE401", "Software Architecture"),
        })
        {
            if (!await _context.Subjects.AnyAsync(subject => subject.Code == code))
                _context.Subjects.Add(new Subject { Code = code, Name = name });
        }

        await _context.SaveChangesAsync();
    }

    private static long P95(IEnumerable<long> values)
    {
        var ordered = values.Order().ToArray();
        return ordered[(int)Math.Ceiling(ordered.Length * 0.95d) - 1];
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record DemoSnapshot(
        int UserCount,
        int RoleAssignmentCount,
        int UsersWithWrongRoleCount,
        int LoginableUserCount,
        int SessionCount,
        int UsersWithoutThreeSessions,
        int InvalidMembershipSessionCount,
        int UserTurnCount,
        int TerminalVariantCount,
        int CompletedCount,
        int FailedCount,
        int SelectedVariantCount,
        int TurnsWithoutExactlyOneSelectedVariant,
        int InvalidMessageShapeCount,
        int ExtraVariantCount,
        int MeasuredCompletionCount,
        int PartialTokenCount,
        int AbsentTokenCount,
        int CitedCompletionCount,
        int NoContextCompletionCount,
        int ContextCountMismatchCount,
        int InvalidContextOrderCount,
        int CitationsFromNonIndexedDocuments,
        int DocumentCount,
        int IndexedDocumentCount,
        int ProcessingDocumentCount,
        int FailedDocumentCount,
        int ActiveDayCount,
        int HistoryDaySpan,
        DateOnly FirstActivityDate,
        IReadOnlyDictionary<string, int> SessionCountsBySubject,
        IReadOnlyDictionary<string, int> TerminalCountsBySubject,
        IReadOnlyDictionary<string, int> CompletedCountsBySubject,
        IReadOnlyDictionary<string, long> P95BySubject,
        string DeterministicFingerprint);
}
