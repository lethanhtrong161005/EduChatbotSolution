using DataAccess.Data;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Repositories;

public class ChatTurnRepository(EduChatAiDbContext context)
{
    private readonly EduChatAiDbContext _context = context;

    public async Task<(ChatMessage UserMessage, ChatMessage AssistantMessage)> CreateExchangeAsync(
        Guid sessionId,
        string userContent,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        _ = await LockSessionAsync(sessionId, cancellationToken)
            ?? throw new EntityNotFoundException("No chat session matched the provided ID.");

        var maximumIndex = await _context.ChatMessages
            .Where(message => message.ChatSessionId == sessionId)
            .MaxAsync(message => message.MessageIndex, cancellationToken) ?? 0;

        if (maximumIndex % 2 != 0)
            throw new InvalidOperationException("The chat session ends with an incomplete logical turn.");

        var sentAt = DateTime.UtcNow;
        var userMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            ChatRole = ChatRole.User,
            Content = userContent,
            RawContent = userContent,
            SentAt = sentAt,
            Status = MessageStatus.Completed,
            MessageIndex = maximumIndex + 1,
        };

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            ChatRole = ChatRole.Assistant,
            SentAt = sentAt,
            Status = MessageStatus.Pending,
            MessageIndex = maximumIndex + 2,
            InReplyToMessage = userMessage,
            VariantIndex = 1,
            IsSelectedVariant = true,
        };

        _context.ChatMessages.AddRange(userMessage, assistantMessage);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (userMessage, assistantMessage);
    }

    public async Task<ChatMessage> ResetFailedAssistantMessageAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        _ = await LockMessageAsync(assistantMessageId, sessionId, cancellationToken)
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        var message = await _context.ChatMessages
            .Include(item => item.GenerationSettings)
            .Include(item => item.GenerationMetrics)
            .Include(item => item.Citations)
                .ThenInclude(citation => citation.CitationOccurrences)
            .SingleOrDefaultAsync(
                item => item.Id == assistantMessageId && item.ChatSessionId == sessionId,
                cancellationToken)
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        if (message.ChatRole != ChatRole.Assistant || message.Status != MessageStatus.Failed)
            throw new EntityConflictException("Only a failed assistant message can be retried.", nameof(ChatMessage.Status));

        if (message.GenerationSettings is not null)
            _context.ChatMessageGenerationSettings.Remove(message.GenerationSettings);
        if (message.GenerationMetrics is not null)
            _context.ChatMessageGenerationMetrics.Remove(message.GenerationMetrics);
        if (message.Citations.Count > 0)
            _context.Citations.RemoveRange(message.Citations);

        message.Content = string.Empty;
        message.RawContent = string.Empty;
        message.GenerationErrors = null;
        message.Status = MessageStatus.Pending;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return message;
    }

    public async Task<ChatMessage> CreateAssistantVariantAsync(
        Guid sessionId,
        Guid completedAssistantMessageId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var source = await _context.ChatMessages.SingleOrDefaultAsync(
            item => item.Id == completedAssistantMessageId && item.ChatSessionId == sessionId,
            cancellationToken)
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        if (source.ChatRole != ChatRole.Assistant || source.Status != MessageStatus.Completed)
            throw new EntityConflictException("Only a completed assistant message can be regenerated.", nameof(ChatMessage.Status));

        if (source.InReplyToMessageId is null)
            throw new InvalidOperationException("The assistant message is missing user reply target.");

        _ = await LockMessageAsync(source.InReplyToMessageId.Value, sessionId, cancellationToken)
            ?? throw new InvalidOperationException("The assistant message has no valid user reply target.");

        var variants = await _context.ChatMessages
            .Where(item => item.ChatSessionId == sessionId && item.InReplyToMessageId == source.InReplyToMessageId)
            .ToListAsync(cancellationToken);

        foreach (var selected in variants.Where(item => item.IsSelectedVariant))
            selected.IsSelectedVariant = false;
        await _context.SaveChangesAsync(cancellationToken);

        var variant = new ChatMessage
        {
            ChatSessionId = sessionId,
            ChatRole = ChatRole.Assistant,
            SentAt = DateTime.UtcNow,
            Status = MessageStatus.Pending,
            MessageIndex = source.MessageIndex,
            InReplyToMessageId = source.InReplyToMessageId,
            VariantIndex = variants.Max(item => item.VariantIndex ?? 0) + 1,
            IsSelectedVariant = true,
        };

        _context.ChatMessages.Add(variant);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return variant;
    }

    public Task<ChatMessage?> GetAssistantVariantAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cancellationToken = default)
        => _context.ChatMessages
            .AsNoTracking()
            .Include(item => item.Citations)
                .ThenInclude(citation => citation.Chunk)
                    .ThenInclude(chunk => chunk.Document)
            .SingleOrDefaultAsync(
                item => item.Id == assistantMessageId &&
                        item.ChatSessionId == sessionId &&
                        item.ChatRole == ChatRole.Assistant,
                cancellationToken);

    public async Task<ChatMessage> SelectAssistantVariantAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var target = await _context.ChatMessages.SingleOrDefaultAsync(
            item => item.Id == assistantMessageId && item.ChatSessionId == sessionId,
            cancellationToken)
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        if (target.ChatRole != ChatRole.Assistant)
            throw new EntityConflictException("Only an assistant variant can be selected.", nameof(ChatMessage.ChatRole));

        if (target.InReplyToMessageId is null)
            throw new InvalidOperationException("The assistant message is missing user reply target.");

        _ = await LockMessageAsync(target.InReplyToMessageId.Value, sessionId, cancellationToken)
            ?? throw new InvalidOperationException("The assistant message has no valid user reply target.");

        var variants = await _context.ChatMessages
            .Where(item => item.ChatSessionId == sessionId && item.InReplyToMessageId == target.InReplyToMessageId)
            .ToListAsync(cancellationToken);

        if (target.IsSelectedVariant)
        {
            await transaction.CommitAsync(cancellationToken);
            return target;
        }

        foreach (var selected in variants.Where(item => item.IsSelectedVariant))
            selected.IsSelectedVariant = false;
        await _context.SaveChangesAsync(cancellationToken);

        target.IsSelectedVariant = true;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return target;
    }

    private Task<ChatSession?> LockSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => _context.ChatSessions
            .FromSqlInterpolated($"SELECT * FROM chat_sessions WHERE id = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<ChatMessage?> LockMessageAsync(Guid messageId, Guid sessionId, CancellationToken cancellationToken)
        => _context.ChatMessages
            .FromSqlInterpolated($"SELECT * FROM chat_messages WHERE id = {messageId} AND chat_session_id = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
}
