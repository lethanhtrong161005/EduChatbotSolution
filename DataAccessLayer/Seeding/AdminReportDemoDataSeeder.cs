using DataAccess.Data;
using Domain.Entities;
using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Cryptography;
using System.Text;

namespace DataAccess.Seeding;

public sealed class AdminReportDemoDataSeeder(EduChatAiDbContext context, TimeProvider timeProvider)
{
    public const string DemoDocumentTitlePrefix = "[Admin Dashboard Demo]";
    private const string DemoEmailSuffix = "@admin-reports-demo.invalid";
    private const long AdvisoryLockKey = 2_026_071_500_01;
    private static readonly Guid DemoNamespace = new("42d7e7de-5878-4d16-8f69-8066f9188f10");
    private static readonly string[] RequiredRoles = ["Student", "Lecturer", "Admin"];
    private static readonly string[] RequiredSubjectCodes = ["DB201", "AI301", "SE401"];

    public async Task SeedAsync(CancellationToken cxlTkn = default)
    {
        IDbContextTransaction? ownedTransaction = null;
        try
        {
            if (context.Database.CurrentTransaction is null)
                ownedTransaction = await context.Database.BeginTransactionAsync(cxlTkn);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", cxlTkn);

            var roles = await context.Roles.AsNoTracking()
                .Where(role => role.Name != null && RequiredRoles.Contains(role.Name))
                .ToDictionaryAsync(role => role.Name!, StringComparer.Ordinal, cxlTkn);
            var missingRoles = RequiredRoles.Where(role => !roles.ContainsKey(role)).ToArray();
            if (missingRoles.Length > 0)
                throw new InvalidOperationException($"Admin report demo data requires roles: {string.Join(", ", missingRoles)}.");

            var subjects = await context.Subjects.AsNoTracking()
                .Where(subject => RequiredSubjectCodes.Contains(subject.Code))
                .ToDictionaryAsync(subject => subject.Code, StringComparer.Ordinal, cxlTkn);
            var missingSubjects = RequiredSubjectCodes.Where(code => !subjects.ContainsKey(code)).ToArray();
            if (missingSubjects.Length > 0)
                throw new InvalidOperationException($"Admin report demo data requires subjects: {string.Join(", ", missingSubjects)}.");

            var bangkokToday = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddHours(7));
            var data = BuildData(roles, subjects, bangkokToday);
            await DeleteExistingDemoDataAsync(data, cxlTkn);
            context.ChangeTracker.Clear();
            await StageStableDemoRootsAsync(data, cxlTkn);

            context.UserRoles.AddRange(data.UserRoles);
            context.Memberships.AddRange(data.Memberships);
            context.ChatSessions.AddRange(data.Sessions);
            context.ChatMessages.AddRange(data.Messages);
            context.ChatMessageGenerationSettings.AddRange(data.Settings);
            context.ChatMessageGenerationMetrics.AddRange(data.Metrics);
            context.ChatMessageContexts.AddRange(data.Contexts);
            context.Citations.AddRange(data.Citations);
            await context.SaveChangesAsync(cxlTkn);

            if (ownedTransaction is not null)
                await ownedTransaction.CommitAsync(cxlTkn);
        }
        catch
        {
            if (ownedTransaction is not null)
                await ownedTransaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }

    private static DemoData BuildData(
        IReadOnlyDictionary<string, ApplicationRole> roles,
        IReadOnlyDictionary<string, Subject> subjects,
        DateOnly bangkokToday)
    {
        var data = new DemoData();
        var users = BuildUsers(roles, data);
        BuildDocuments(subjects, users["Admin-1"], bangkokToday, data);

        var sessionPlans = BuildSessionPlans(users);
        BuildMemberships(sessionPlans, subjects, data);
        var activityTimes = BuildActivityTimes(bangkokToday);
        var chunksBySubject = data.Chunks
            .GroupBy(chunk => data.DocumentSubjectCodes[chunk.DocumentId])
            .ToDictionary(group => group.Key, group => group.OrderBy(chunk => chunk.DocumentId).ThenBy(chunk => chunk.ChunkIndex).ToArray());

        var turnOrdinalByBucket = RequiredSubjectCodes.Append(FlexibleBucket.Code)
            .ToDictionary(code => code, _ => 0, StringComparer.Ordinal);
        var completedOrdinalByBucket = RequiredSubjectCodes.Append(FlexibleBucket.Code)
            .ToDictionary(code => code, _ => 0, StringComparer.Ordinal);
        var globalTurnOrdinal = 0;

        for (var sessionOrdinal = 0; sessionOrdinal < sessionPlans.Count; sessionOrdinal++)
        {
            var plan = sessionPlans[sessionOrdinal];
            var subject = plan.SubjectCode is null ? null : subjects[plan.SubjectCode];
            var sessionId = CreateDemoId($"session:{sessionOrdinal + 1:D2}");
            var session = new ChatSession
            {
                Id = sessionId,
                UserId = plan.User.Id,
                SubjectId = subject?.Id,
                Title = $"{DemoDocumentTitlePrefix} {plan.SubjectCode ?? "Flexible subjects"} session {sessionOrdinal + 1:D2}",
            };
            data.Sessions.Add(session);

            for (var turn = 0; turn < 5; turn++)
            {
                var bucket = plan.SubjectCode ?? FlexibleBucket.Code;
                var bucketTurnOrdinal = turnOrdinalByBucket[bucket]++;
                var timestamp = activityTimes[globalTurnOrdinal++];
                var userMessageId = CreateDemoId($"message:user:{sessionOrdinal + 1:D2}:{turn + 1:D2}");
                var userMessage = new ChatMessage
                {
                    Id = userMessageId,
                    ChatSessionId = sessionId,
                    ChatRole = ChatRole.User,
                    Content = $"Demo question {turn + 1} for {plan.SubjectCode ?? "my assigned subjects"}",
                    RawContent = $"Demo question {turn + 1} for {plan.SubjectCode ?? "my assigned subjects"}",
                    SentAt = timestamp,
                    Status = MessageStatus.Completed,
                    MessageIndex = turn * 2 + 1,
                    IsSelectedVariant = false,
                };
                data.Messages.Add(userMessage);

                var shape = BucketShape.For(bucket);
                var retryReplacement = bucketTurnOrdinal < shape.RetryReplacementCount;
                var retainedCompleted = bucketTurnOrdinal >= shape.RetryReplacementCount
                    && bucketTurnOrdinal < shape.RetryReplacementCount + shape.RetainedCompletedCount;
                var selectedFailure = bucketTurnOrdinal >= shape.RetryReplacementCount + shape.RetainedCompletedCount
                    && bucketTurnOrdinal < shape.RetryReplacementCount + shape.RetainedCompletedCount + shape.SelectedFailureCount;

                AddAssistantVariant(
                    plan,
                    sessionOrdinal,
                    turn,
                    userMessage,
                    variantIndex: 1,
                    status: retryReplacement || selectedFailure ? MessageStatus.Failed : MessageStatus.Completed,
                    selected: !retryReplacement && !retainedCompleted,
                    timestamp.AddSeconds(4),
                    chunksBySubject,
                    shape,
                    completedOrdinalByBucket,
                    data);

                if (retryReplacement || retainedCompleted)
                {
                    AddAssistantVariant(
                        plan,
                        sessionOrdinal,
                        turn,
                        userMessage,
                        variantIndex: 2,
                        status: MessageStatus.Completed,
                        selected: true,
                        timestamp.AddSeconds(8),
                        chunksBySubject,
                        shape,
                        completedOrdinalByBucket,
                        data);
                }
            }
        }

        return data;
    }

    private static Dictionary<string, ApplicationUser> BuildUsers(
        IReadOnlyDictionary<string, ApplicationRole> roles,
        DemoData data)
    {
        var result = new Dictionary<string, ApplicationUser>(StringComparer.Ordinal);
        foreach (var (roleName, count) in new[] { ("Student", 8), ("Lecturer", 3), ("Admin", 1) })
        {
            for (var index = 1; index <= count; index++)
            {
                var key = $"{roleName}-{index}";
                var email = $"admin-report-demo-{roleName.ToLowerInvariant()}-{index:D2}{DemoEmailSuffix}";
                var user = new ApplicationUser
                {
                    Id = CreateDemoId($"user:{key}"),
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    FullName = $"[Admin Dashboard Demo] {roleName} {index:D2}",
                    EmailConfirmed = false,
                    IsActive = true,
                    LockoutEnabled = true,
                    SecurityStamp = CreateDemoId($"security-stamp:{key}").ToString("N"),
                    ConcurrencyStamp = CreateDemoId($"concurrency-stamp:{key}").ToString("N"),
                    UpdatedAt = DateTimeOffset.UnixEpoch,
                };
                result[key] = user;
                data.Users.Add(user);
                data.UserRoles.Add(new ApplicationUserRole { UserId = user.Id, RoleId = roles[roleName].Id });
            }
        }

        return result;
    }

    private static void BuildDocuments(
        IReadOnlyDictionary<string, Subject> subjects,
        ApplicationUser uploader,
        DateOnly bangkokToday,
        DemoData data)
    {
        var definitions = new (string SubjectCode, DocumentStatus Status)[]
        {
            ("DB201", DocumentStatus.Indexed),
            ("DB201", DocumentStatus.Indexed),
            ("DB201", DocumentStatus.Indexed),
            ("DB201", DocumentStatus.Indexed),
            ("DB201", DocumentStatus.Embedding),
            ("DB201", DocumentStatus.Failed),
            ("AI301", DocumentStatus.Indexed),
            ("AI301", DocumentStatus.Indexed),
            ("AI301", DocumentStatus.Chunking),
            ("AI301", DocumentStatus.Failed),
            ("SE401", DocumentStatus.Indexed),
            ("SE401", DocumentStatus.Parsing),
        };

        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            var documentId = CreateDemoId($"document:{index + 1:D2}");
            var isIndexed = definition.Status == DocumentStatus.Indexed;
            var document = new Document
            {
                Id = documentId,
                SubjectId = subjects[definition.SubjectCode].Id,
                UploaderId = uploader.Id,
                Title = $"{DemoDocumentTitlePrefix} {definition.SubjectCode} source {index + 1:D2}",
                Description = "Synthetic source used only for the development admin dashboard demonstration.",
                OriginalFileName = $"admin-report-demo-{definition.SubjectCode.ToLowerInvariant()}-{index + 1:D2}.pdf",
                FileType = FileType.PDF,
                FileSize = 50_000 + index * 1_000,
                StorageMethod = FileStorageMethod.Unspecified,
                Status = definition.Status,
                ParserUsed = isIndexed ? "DemoParser" : null,
                IndexingErrors = definition.Status == DocumentStatus.Failed ? "Synthetic indexing failure for dashboard demonstration." : null,
                IndexedChunkingStrategy = isIndexed ? "FixedLength" : null,
                IndexedChunkSize = isIndexed ? 1_000 : null,
                IndexedChunkOverlap = isIndexed ? 200 : null,
                IndexedEmbeddingModel = isIndexed ? "demo-embedding-1024" : null,
                UploadedAt = ToUtc(bangkokToday.AddDays(-58 + index), 8, 0),
            };
            data.Documents.Add(document);
            data.DocumentSubjectCodes[documentId] = definition.SubjectCode;

            if (!isIndexed) continue;
            for (var chunkIndex = 0; chunkIndex < 2; chunkIndex++)
            {
                data.Chunks.Add(new Chunk
                {
                    Id = CreateDemoId($"chunk:{index + 1:D2}:{chunkIndex}"),
                    DocumentId = documentId,
                    ChunkIndex = chunkIndex,
                    ChunkText = $"{definition.SubjectCode} demo context {index + 1:D2}.{chunkIndex}: grounded historical dashboard content.",
                    StartPageNumber = chunkIndex + 1,
                    EndPageNumber = chunkIndex + 1,
                    StartSectionTitle = "Demo section",
                    EndSectionTitle = "Demo section",
                    ChunkingStrategy = "FixedLength",
                    EmbeddingModel = "demo-embedding-1024",
                    TokenCount = 24,
                });
            }
        }
    }

    private static List<SessionPlan> BuildSessionPlans(IReadOnlyDictionary<string, ApplicationUser> users)
    {
        var definitions = new Dictionary<string, string?[]>(StringComparer.Ordinal)
        {
            ["Student-1"] = ["DB201", "DB201", null],
            ["Student-2"] = ["DB201", "DB201", null],
            ["Student-3"] = ["DB201", "DB201", "AI301"],
            ["Student-4"] = ["DB201", "DB201", "AI301"],
            ["Student-5"] = ["DB201", "DB201", "AI301"],
            ["Student-6"] = ["DB201", "DB201", "AI301"],
            ["Student-7"] = ["DB201", "AI301", "SE401"],
            ["Student-8"] = ["DB201", "AI301", "SE401"],
            ["Lecturer-1"] = ["DB201", "AI301", null],
            ["Lecturer-2"] = ["DB201", "AI301", null],
            ["Lecturer-3"] = ["DB201", "DB201", "SE401"],
            ["Admin-1"] = ["DB201", "DB201", "SE401"],
        };

        return [.. definitions.SelectMany(pair => pair.Value.Select(subjectCode =>
            new SessionPlan(pair.Key, users[pair.Key], subjectCode)))];
    }

    private static void BuildMemberships(
        IReadOnlyList<SessionPlan> plans,
        IReadOnlyDictionary<string, Subject> subjects,
        DemoData data)
    {
        foreach (var userPlans in plans.Where(plan => !plan.UserKey.StartsWith("Admin-", StringComparison.Ordinal))
                     .GroupBy(plan => plan.UserKey))
        {
            var role = userPlans.Key.StartsWith("Student-", StringComparison.Ordinal)
                ? MembershipRole.Student
                : MembershipRole.Lecturer;
            foreach (var subjectCode in userPlans.Where(plan => plan.SubjectCode is not null)
                         .Select(plan => plan.SubjectCode!)
                         .Distinct(StringComparer.Ordinal))
            {
                data.Memberships.Add(new Membership
                {
                    Id = CreateDemoId($"membership:{userPlans.Key}:{subjectCode}"),
                    SubjectId = subjects[subjectCode].Id,
                    UserId = userPlans.First().User.Id,
                    Role = role,
                    AssignedAt = DateTime.UnixEpoch,
                });
            }
        }
    }

    private static DateTime[] BuildActivityTimes(DateOnly bangkokToday)
    {
        var zeroDayOffsets = new HashSet<int> { 6, 13, 20, 27, 34, 41, 48, 55 };
        var activityDays = Enumerable.Range(0, 60)
            .Where(offset => !zeroDayOffsets.Contains(offset))
            .Select(offset => bangkokToday.AddDays(-59 + offset))
            .ToArray();
        var result = new List<DateTime>(180);
        for (var index = 0; index < activityDays.Length; index++)
        {
            var count = index < 24 ? 4 : 3;
            for (var item = 0; item < count; item++)
                result.Add(ToUtc(activityDays[index], 9 + item, index % 6 * 7));
        }

        return [.. result];
    }

    private static void AddAssistantVariant(
        SessionPlan plan,
        int sessionOrdinal,
        int turn,
        ChatMessage userMessage,
        int variantIndex,
        MessageStatus status,
        bool selected,
        DateTime sentAt,
        IReadOnlyDictionary<string, Chunk[]> chunksBySubject,
        BucketShape shape,
        IDictionary<string, int> completedOrdinalByBucket,
        DemoData data)
    {
        var bucket = plan.SubjectCode ?? FlexibleBucket.Code;
        var assistantId = CreateDemoId($"message:assistant:{sessionOrdinal + 1:D2}:{turn + 1:D2}:{variantIndex}");
        var assistant = new ChatMessage
        {
            Id = assistantId,
            ChatSessionId = userMessage.ChatSessionId,
            ChatRole = ChatRole.Assistant,
            Content = status == MessageStatus.Completed
                ? $"Synthetic grounded answer variant {variantIndex} for the admin dashboard demo."
                : string.Empty,
            RawContent = status == MessageStatus.Completed
                ? $"Synthetic grounded answer variant {variantIndex} for the admin dashboard demo."
                : string.Empty,
            SentAt = sentAt,
            Status = status,
            GenerationErrors = status == MessageStatus.Failed ? "Synthetic provider failure for dashboard demonstration." : null,
            MessageIndex = userMessage.MessageIndex + 1,
            InReplyToMessageId = userMessage.Id,
            VariantIndex = variantIndex,
            IsSelectedVariant = selected,
        };
        data.Messages.Add(assistant);
        data.Settings.Add(new ChatMessageGenerationSettings
        {
            Id = assistantId,
            EmbeddingModel = "demo-embedding-1024",
            TopK = 8,
            SimilarityThreshold = 0.72,
            LlmModel = "demo-chat-model",
            Temperature = 0.2f,
            SystemPrompt = "Use only the supplied subject context.",
            ContextPrompt = "Context:\n{context}",
            NoContextRetrievedPrompt = "No matching subject context was found.",
            CitationExtractionTemperature = 0f,
            CitationExtractionPrompt = "Extract citations from the supplied context.",
            MaxContextChunks = 4,
            MaxHistoryMessages = 10,
        });

        if (status != MessageStatus.Completed) return;
        var completedOrdinal = completedOrdinalByBucket[bucket]++;
        var noContext = completedOrdinal < shape.NoContextCount;
        var contextCount = noContext ? 0 : 2;
        var measured = completedOrdinal < shape.MeasuredTokenCount;
        var partial = completedOrdinal >= shape.MeasuredTokenCount
            && completedOrdinal < shape.MeasuredTokenCount + shape.PartialTokenCount;
        int? promptTokens = measured ? 220 + completedOrdinal * 3 : partial && completedOrdinal % 2 == 0 ? 250 + completedOrdinal : null;
        int? completionTokens = measured ? 55 + completedOrdinal % 20 : partial && completedOrdinal % 2 != 0 ? 70 + completedOrdinal % 10 : null;
        var latency = completedOrdinal % 10 == 0
            ? shape.P95LatencyMs
            : shape.P95LatencyMs - 100 - completedOrdinal % 5 * 100;
        data.Metrics.Add(new ChatMessageGenerationMetrics
        {
            Id = assistantId,
            RetrievedChunkCount = contextCount == 0 ? 0 : 3,
            ContextChunkCount = contextCount,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            RetrievalTimeMs = contextCount == 0 ? 35 : 90 + completedOrdinal % 4 * 10,
            TimeToFirstTokenMs = completedOrdinal % 7 == 0 ? null : 280 + completedOrdinal % 6 * 25,
            TotalResponseTimeMs = latency,
            TokensPerSecond = measured ? completionTokens!.Value / (latency / 1_000d) : null,
        });

        if (noContext) return;
        var contextSubjectCode = plan.SubjectCode ?? plan.AccessibleSubjectCode;
        var chunks = chunksBySubject[contextSubjectCode];
        for (var contextIndex = 0; contextIndex < contextCount; contextIndex++)
        {
            data.Contexts.Add(new ChatMessageContext
            {
                Id = CreateDemoId($"context:{assistantId:N}:{contextIndex}"),
                ChatMessageId = assistantId,
                ContextIndex = contextIndex,
                ContextText = chunks[contextIndex].ChunkText,
            });
        }

        var cited = completedOrdinal >= shape.NoContextCount
            && completedOrdinal < shape.NoContextCount + shape.CitedCount;
        if (!cited) return;
        data.Citations.Add(new Citation
        {
            Id = CreateDemoId($"citation:{assistantId:N}:1"),
            ChatMessageId = assistantId,
            ChunkId = chunks[0].Id,
            CitationIndex = 1,
            SimilarityScore = 0.91,
            LocationInDocument = "Page: 1 • Section: Demo section",
        });
    }

    private async Task DeleteExistingDemoDataAsync(DemoData data, CancellationToken cxlTkn)
    {
        var currentUserIds = data.Users.Select(entity => entity.Id).ToArray();
        var userIds = await context.Users.AsNoTracking()
            .Where(entity => currentUserIds.Contains(entity.Id)
                || entity.Email != null && entity.Email.EndsWith(DemoEmailSuffix))
            .Select(entity => entity.Id)
            .ToArrayAsync(cxlTkn);
        var sessionIds = await context.ChatSessions.AsNoTracking()
            .Where(entity => userIds.Contains(entity.UserId))
            .Select(entity => entity.Id)
            .ToArrayAsync(cxlTkn);
        var messageIds = await context.ChatMessages.AsNoTracking()
            .Where(entity => sessionIds.Contains(entity.ChatSessionId))
            .Select(entity => entity.Id)
            .ToArrayAsync(cxlTkn);
        var citationIds = await context.Citations.AsNoTracking()
            .Where(entity => messageIds.Contains(entity.ChatMessageId))
            .Select(entity => entity.Id)
            .ToArrayAsync(cxlTkn);

        await context.CitationOccurrences.Where(entity => citationIds.Contains(entity.CitationId)).ExecuteDeleteAsync(cxlTkn);
        await context.Citations.Where(entity => messageIds.Contains(entity.ChatMessageId)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatMessageContexts.Where(entity => messageIds.Contains(entity.ChatMessageId)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatMessageGenerationMetrics.Where(entity => messageIds.Contains(entity.Id)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatMessageGenerationSettings.Where(entity => messageIds.Contains(entity.Id)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatMessages.Where(entity => sessionIds.Contains(entity.ChatSessionId)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatSessionTitleGenerationMetrics.Where(entity => sessionIds.Contains(entity.Id)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatSessionTitleGenerationSettings.Where(entity => sessionIds.Contains(entity.Id)).ExecuteDeleteAsync(cxlTkn);
        await context.ChatSessions.Where(entity => userIds.Contains(entity.UserId)).ExecuteDeleteAsync(cxlTkn);
        await context.Memberships.Where(entity => userIds.Contains(entity.UserId)).ExecuteDeleteAsync(cxlTkn);
        await context.UserRoles.Where(entity => userIds.Contains(entity.UserId)).ExecuteDeleteAsync(cxlTkn);
    }

    private async Task StageStableDemoRootsAsync(DemoData data, CancellationToken cxlTkn)
    {
        var desiredUserIds = data.Users.Select(entity => entity.Id).ToArray();
        var existingUsers = await context.Users
            .Where(entity => desiredUserIds.Contains(entity.Id))
            .ToDictionaryAsync(entity => entity.Id, cxlTkn);
        foreach (var desired in data.Users)
        {
            if (existingUsers.TryGetValue(desired.Id, out var existing))
                context.Entry(existing).CurrentValues.SetValues(desired);
            else
                context.Users.Add(desired);
        }

        var desiredDocumentIds = data.Documents.Select(entity => entity.Id).ToArray();
        var existingDocuments = await context.Documents
            .Where(entity => desiredDocumentIds.Contains(entity.Id))
            .ToDictionaryAsync(entity => entity.Id, cxlTkn);
        foreach (var desired in data.Documents)
        {
            if (existingDocuments.TryGetValue(desired.Id, out var existing))
            {
                var createdAt = existing.CreatedAt;
                var updatedAt = existing.UpdatedAt;
                context.Entry(existing).CurrentValues.SetValues(desired);
                existing.CreatedAt = createdAt;
                existing.UpdatedAt = updatedAt;
            }
            else
            {
                context.Documents.Add(desired);
            }
        }

        var desiredChunkIds = data.Chunks.Select(entity => entity.Id).ToArray();
        var existingChunks = await context.Chunks
            .Where(entity => desiredChunkIds.Contains(entity.Id))
            .ToDictionaryAsync(entity => entity.Id, cxlTkn);
        foreach (var desired in data.Chunks)
        {
            if (existingChunks.TryGetValue(desired.Id, out var existing))
            {
                var createdAt = existing.CreatedAt;
                var updatedAt = existing.UpdatedAt;
                context.Entry(existing).CurrentValues.SetValues(desired);
                existing.CreatedAt = createdAt;
                existing.UpdatedAt = updatedAt;
            }
            else
            {
                context.Chunks.Add(desired);
            }
        }
    }

    private static Guid CreateDemoId(string key)
    {
        var namespaceBytes = DemoNamespace.ToByteArray();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var input = new byte[namespaceBytes.Length + keyBytes.Length];
        namespaceBytes.CopyTo(input, 0);
        keyBytes.CopyTo(input, namespaceBytes.Length);
        var hash = SHA256.HashData(input);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16));
    }

    private static DateTime ToUtc(DateOnly date, int hour, int minute) =>
        DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)).AddHours(-7), DateTimeKind.Utc);

    private sealed record SessionPlan(string UserKey, ApplicationUser User, string? SubjectCode)
    {
        public string AccessibleSubjectCode => SubjectCode ?? UserKey switch
        {
            "Lecturer-1" or "Lecturer-2" => "DB201",
            _ => "DB201",
        };
    }

    private sealed record BucketShape(
        int RetryReplacementCount,
        int RetainedCompletedCount,
        int SelectedFailureCount,
        int NoContextCount,
        int MeasuredTokenCount,
        int PartialTokenCount,
        int CitedCount,
        long P95LatencyMs)
    {
        public static BucketShape For(string code) => code switch
        {
            "DB201" => new(4, 8, 0, 8, 89, 6, 88, 1_800),
            "AI301" => new(3, 1, 2, 10, 32, 2, 29, 2_400),
            "SE401" => new(2, 0, 2, 3, 15, 1, 14, 3_200),
            FlexibleBucket.Code => new(1, 1, 0, 2, 17, 1, 17, 2_100),
            _ => throw new ArgumentOutOfRangeException(nameof(code)),
        };
    }

    private static class FlexibleBucket
    {
        public const string Code = "__flexible__";
    }

    private sealed class DemoData
    {
        public List<ApplicationUser> Users { get; } = [];
        public List<ApplicationUserRole> UserRoles { get; } = [];
        public List<Membership> Memberships { get; } = [];
        public List<Document> Documents { get; } = [];
        public Dictionary<Guid, string> DocumentSubjectCodes { get; } = [];
        public List<Chunk> Chunks { get; } = [];
        public List<ChatSession> Sessions { get; } = [];
        public List<ChatMessage> Messages { get; } = [];
        public List<ChatMessageGenerationSettings> Settings { get; } = [];
        public List<ChatMessageGenerationMetrics> Metrics { get; } = [];
        public List<ChatMessageContext> Contexts { get; } = [];
        public List<Citation> Citations { get; } = [];
    }
}
