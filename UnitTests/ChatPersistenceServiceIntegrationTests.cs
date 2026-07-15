using AutoMapper;
using Business.Services.AI.Chat;
using DataAccess.Data;
using DataAccess.UnitOfWork;
using Domain.Contracts.DTOs;
using Domain.Entities;
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
            context.ChatSessions.Add(new ChatSession { Id = sessionId, UserId = TestUserId, Title = "Context snapshots" });
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
                    new ChunkRetrieval { ChunkId = Guid.NewGuid(), ChunkText = "First", SimilarityScore = .9 },
                    new ChunkRetrieval { ChunkId = Guid.NewGuid(), ChunkText = "Second", SimilarityScore = .8 },
                ],
                [
                    new RetrievedContextSnapshot { ContextIndex = 0, ContextText = "First" },
                    new RetrievedContextSnapshot { ContextIndex = 1, ContextText = "Second" },
                ],
                [],
                Settings(),
                Metrics());
        }

        await using var assertionContext = CreateContext();
        var contexts = await assertionContext.ChatMessageContexts.AsNoTracking()
            .Where(item => item.ChatMessageId == assistantMessageId)
            .OrderBy(item => item.ContextIndex)
            .ToListAsync();
        var metrics = await assertionContext.ChatMessageGenerationMetrics.AsNoTracking().SingleAsync(item => item.Id == assistantMessageId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contexts.Select(item => item.ContextIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(contexts.Select(item => item.ContextText), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(metrics.ContextChunkCount, Is.EqualTo(2));
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
        EmbeddingModel = "embed",
        TopK = 5,
        SimilarityThreshold = .5,
        LlmModel = "chat",
        Temperature = .2F,
        SystemPrompt = "system",
        ContextPrompt = "context",
        NoContextRetrievedPrompt = "none",
        CitationExtractionTemperature = 0,
        CitationExtractionPrompt = "citations",
        MaxContextChunks = 2,
        MaxHistoryMessages = 10,
    };

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
