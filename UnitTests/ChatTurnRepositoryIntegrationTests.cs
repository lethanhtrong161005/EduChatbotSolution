using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace UnitTests;

[NonParallelizable]
public class ChatTurnRepositoryIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private string _connectionString = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");

        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO users (
                id, full_name, is_active, email_confirmed, phone_number_confirmed,
                two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES (
                '00000000-0000-0000-0000-000000000001', 'Phase 2 Verify', TRUE, TRUE, FALSE,
                FALSE, FALSE, 0)
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    [Test]
    public async Task CreateExchange_AllocatesUniqueAdjacentSlotsUnderConcurrency()
    {
        var sessionId = await CreateSessionAsync();

        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var firstRepository = new ChatTurnRepository(firstContext);
        var secondRepository = new ChatTurnRepository(secondContext);

        await Task.WhenAll(
            firstRepository.CreateExchangeAsync(sessionId, "First"),
            secondRepository.CreateExchangeAsync(sessionId, "Second"));

        await using var assertionContext = CreateContext();
        var indices = await assertionContext.ChatMessages
            .Where(message => message.ChatSessionId == sessionId)
            .OrderBy(message => message.MessageIndex)
            .Select(message => message.MessageIndex)
            .ToListAsync();

        Assert.That(indices, Is.EqualTo(new int?[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task ResetFailedAssistant_KeepsIdentityAndClearsGeneratedState()
    {
        var sessionId = await CreateSessionAsync();
        await using var context = CreateContext();
        var repository = new ChatTurnRepository(context);
        var exchange = await repository.CreateExchangeAsync(sessionId, "Question");

        exchange.AssistantMessage.Status = MessageStatus.Failed;
        exchange.AssistantMessage.Content = "partial";
        exchange.AssistantMessage.RawContent = "partial";
        exchange.AssistantMessage.GenerationErrors = "failure";
        await context.SaveChangesAsync();

        var reset = await repository.ResetFailedAssistantMessageAsync(sessionId, exchange.AssistantMessage.Id);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Id, Is.EqualTo(exchange.AssistantMessage.Id));
            Assert.That(reset.Status, Is.EqualTo(MessageStatus.Pending));
            Assert.That(reset.Content, Is.Empty);
            Assert.That(reset.RawContent, Is.Empty);
            Assert.That(reset.GenerationErrors, Is.Null);
        });
    }

    [Test]
    public async Task ResetFailedAssistant_AllowsOnlyOneConcurrentReset()
    {
        var sessionId = await CreateSessionAsync();
        Guid assistantMessageId;

        await using (var setupContext = CreateContext())
        {
            var setupRepository = new ChatTurnRepository(setupContext);
            var exchange = await setupRepository.CreateExchangeAsync(sessionId, "Question");
            exchange.AssistantMessage.Status = MessageStatus.Failed;
            await setupContext.SaveChangesAsync();
            assistantMessageId = exchange.AssistantMessage.Id;
        }

        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var outcomes = await Task.WhenAll(
            AttemptResetAsync(new ChatTurnRepository(firstContext), sessionId, assistantMessageId),
            AttemptResetAsync(new ChatTurnRepository(secondContext), sessionId, assistantMessageId));

        Assert.Multiple(() =>
        {
            Assert.That(outcomes.Count(exception => exception is null), Is.EqualTo(1));
            Assert.That(outcomes.Count(exception => exception is Domain.Exceptions.EntityConflictException), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ResetFailedAssistant_LocksRowBeforeCheckingStatus()
    {
        var sessionId = await CreateSessionAsync();
        Guid assistantMessageId;

        await using (var setupContext = CreateContext())
        {
            var setupRepository = new ChatTurnRepository(setupContext);
            var exchange = await setupRepository.CreateExchangeAsync(sessionId, "Question");
            exchange.AssistantMessage.Status = MessageStatus.Failed;
            await setupContext.SaveChangesAsync();
            assistantMessageId = exchange.AssistantMessage.Id;
        }

        var interceptor = new CommandCaptureInterceptor();
        await using var context = CreateContext(interceptor);
        await new ChatTurnRepository(context).ResetFailedAssistantMessageAsync(sessionId, assistantMessageId);

        Assert.That(interceptor.Commands.Any(command =>
            command.Contains("FROM chat_messages", StringComparison.OrdinalIgnoreCase) &&
            command.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task RegenerateAndSelect_PreserveTurnAndExactlyOneSelection()
    {
        var sessionId = await CreateSessionAsync();
        await using var context = CreateContext();
        var repository = new ChatTurnRepository(context);
        var exchange = await repository.CreateExchangeAsync(sessionId, "Question");

        exchange.AssistantMessage.Status = MessageStatus.Completed;
        await context.SaveChangesAsync();

        var secondVariant = await repository.CreateAssistantVariantAsync(sessionId, exchange.AssistantMessage.Id);

        Assert.Multiple(() =>
        {
            Assert.That(secondVariant.MessageIndex, Is.EqualTo(exchange.AssistantMessage.MessageIndex));
            Assert.That(secondVariant.VariantIndex, Is.EqualTo(2));
            Assert.That(secondVariant.IsSelectedVariant, Is.True);
            Assert.That(exchange.AssistantMessage.IsSelectedVariant, Is.False);
        });

        await repository.SelectAssistantVariantAsync(sessionId, exchange.AssistantMessage.Id);

        var selected = await context.ChatMessages
            .Where(message => message.InReplyToMessageId == exchange.UserMessage.Id && message.IsSelectedVariant)
            .SingleAsync();
        Assert.That(selected.Id, Is.EqualTo(exchange.AssistantMessage.Id));
    }

    private async Task<Guid> CreateSessionAsync()
    {
        await using var context = CreateContext();
        var sessionId = Guid.NewGuid();
        context.ChatSessions.Add(new ChatSession
        {
            Id = sessionId,
            UserId = TestUserId,
            Title = "Phase 2 integration",
        });
        await context.SaveChangesAsync();
        return sessionId;
    }

    private static async Task<Exception?> AttemptResetAsync(
        ChatTurnRepository repository,
        Guid sessionId,
        Guid assistantMessageId)
    {
        try
        {
            await repository.ResetFailedAssistantMessageAsync(sessionId, assistantMessageId);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private EduChatAiDbContext CreateContext(DbCommandInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention();
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        return new EduChatAiDbContext(builder.Options);
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
