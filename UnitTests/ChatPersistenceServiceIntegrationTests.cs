using AutoMapper;
using Business.Services.AI.Chat;
using DataAccess.Data;
using DataAccess.UnitOfWork;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Presentation.Mappings;

namespace UnitTests;

[NonParallelizable]
public class ChatPersistenceServiceIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private string _connectionString = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString)) Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");

        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO users (
                id, full_name, is_active, email_confirmed, phone_number_confirmed,
                two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES (
                '00000000-0000-0000-0000-000000000001', 'Context Snapshot Verify', TRUE, TRUE, FALSE,
                FALSE, FALSE, 0)
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    [Test]
    public async Task CompleteAssistantMessage_PersistsOrderedContextSnapshotsAndContextCount()
    {
        var sessionId = Guid.NewGuid();
        Guid assistantMessageId;

        await using (var context = CreateContext())
        {
            var subject = context.Subjects.Add(new Subject { Code = $"CTX-{Guid.NewGuid():N}"[..20], Name = "Context snapshots" }).Entity;
            var document = context.Documents.Add(new Document { Subject = subject, UploaderId = TestUserId, Title = "Database", OriginalFileName = "database.pdf", FileType = FileType.PDF, Status = DocumentStatus.Indexed }).Entity;
            var first = context.Chunks.Add(Chunk(document, 1, "First")).Entity;
            var second = context.Chunks.Add(Chunk(document, 2, "Second")).Entity;
            context.ChatSessions.Add(new ChatSession { Id = sessionId, UserId = TestUserId, Subject = subject, Title = "Context snapshots" });
            await context.SaveChangesAsync();

            await using var unitOfWork = new UnitOfWork(context);
            var exchange = await unitOfWork.ChatTurns.CreateExchangeAsync(sessionId, "Question");
            assistantMessageId = exchange.AssistantMessage.Id;
            var service = new ChatPersistenceService(unitOfWork, CreateMapper());

            await service.CompleteAssistantMessageAsync(
                assistantMessageId,
                "Answer",
                "Raw answer",
                [
                    Retrieval(first, document, subject, .9),
                    Retrieval(second, document, subject, .8),
                ],
                [
                    Snapshot(first, document, subject, 1, .9),
                    Snapshot(second, document, subject, 2, .8),
                ],
                [new NormalizedRequestMessageSnapshot { MessageOrder = 1, Role = "system", Content = "system" }, new NormalizedRequestMessageSnapshot { MessageOrder = 2, Role = "user", Content = "Question" }],
                [new ResolvedSubjectSnapshot { SubjectOrder = 1, SubjectId = subject.Id, SubjectCode = subject.Code, SubjectName = subject.Name }],
                [],
                Settings(),
                Metrics());
        }

        await using var assertionContext = CreateContext();
        var contexts = await assertionContext.ChatMessageContexts.AsNoTracking()
            .Where(item => item.ChatMessageId == assistantMessageId)
            .OrderBy(item => item.RetrievalRank)
            .ToListAsync();
        var metrics = await assertionContext.ChatMessageGenerationMetrics.AsNoTracking().SingleAsync(item => item.Id == assistantMessageId);
        var message = await assertionContext.ChatMessages.AsNoTracking().SingleAsync(item => item.Id == assistantMessageId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contexts.Select(item => item.RetrievalRank), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(contexts.Select(item => item.ChunkText), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(contexts.All(item => item.SourceChunkId.HasValue && item.SourceDocumentId.HasValue && item.SourceSubjectId.HasValue), Is.True);
            Assert.That(metrics.ContextChunkCount, Is.EqualTo(2));
            Assert.That(message.ReconstructionCompleteness, Is.EqualTo(ReconstructionCompleteness.Complete));
            Assert.That(await assertionContext.ChatMessageRequestMessages.CountAsync(item => item.ChatMessageId == assistantMessageId), Is.EqualTo(2));
            Assert.That(await assertionContext.ChatMessageSubjectSnapshots.CountAsync(item => item.ChatMessageId == assistantMessageId), Is.EqualTo(1));
        }
    }

    private EduChatAiDbContext CreateContext() => new(
        new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options);

    private static IMapper CreateMapper() => new MapperConfiguration(
        config => config.AddProfile<ChatMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    private static ChatGenerationSettings Settings() => new()
    {
        EmbeddingProvider = "test",
        EmbeddingModel = "embed",
        TopK = 5,
        SimilarityThreshold = .5,
        LlmProvider = "test",
        LlmModel = "chat",
        Temperature = .2F,
        SystemPrompt = "system",
        ContextPrompt = "context",
        NoContextRetrievedPrompt = "none",
        CitationExtractionTemperature = 0,
        CitationExtractionPrompt = "citations",
        MaxContextChunks = 2,
        MaxHistoryMessages = 10,
        ReasoningEffort = "low",
        ReasoningOutput = "none",
    };

    private static Chunk Chunk(Document document, int index, string text) => new() { Document = document, ChunkIndex = index, ChunkText = text, ChunkingStrategy = "FixedLength", ChunkSize = 1000, ChunkOverlap = 200, EmbeddingModel = "embed" };

    private static ChunkRetrieval Retrieval(Chunk chunk, Document document, Subject subject, double score) => new() { ChunkId = chunk.Id, DocumentId = document.Id, SubjectId = subject.Id, ChunkIndex = chunk.ChunkIndex, ChunkText = chunk.ChunkText, SimilarityScore = score, DocumentTitle = document.Title, DocumentFileName = document.OriginalFileName, SubjectCode = subject.Code, SubjectName = subject.Name, ChunkingStrategy = chunk.ChunkingStrategy, ChunkSize = chunk.ChunkSize, ChunkOverlap = chunk.ChunkOverlap, EmbeddingModel = chunk.EmbeddingModel };

    private static RetrievedContextSnapshot Snapshot(Chunk chunk, Document document, Subject subject, int rank, double score) => new() { RetrievalRank = rank, PromptOrder = rank, WasIncludedInPrompt = true, ChunkId = chunk.Id, SourceDocumentId = document.Id, SourceSubjectId = subject.Id, ChunkIndex = chunk.ChunkIndex, ChunkText = chunk.ChunkText, SimilarityScore = score, DocumentTitle = document.Title, DocumentFileName = document.OriginalFileName, SubjectCode = subject.Code, SubjectName = subject.Name, ChunkingStrategy = chunk.ChunkingStrategy, ChunkSize = chunk.ChunkSize, ChunkOverlap = chunk.ChunkOverlap, EmbeddingModel = chunk.EmbeddingModel };

    private static ChatGenerationMetrics Metrics() => new()
    {
        PromptTokens = 20,
        CompletionTokens = 5,
        RetrievalTimeMs = 10,
        TimeToFirstTokenMs = 20,
        TotalResponseTimeMs = 100,
        TokensPerSecond = 50,
    };
}
