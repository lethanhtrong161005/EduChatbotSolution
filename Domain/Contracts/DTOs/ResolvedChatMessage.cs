using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record ResolvedChatMessage
{
    public required Guid Id { get; init; }

    public required ChatRole ChatRole { get; init; }

    public required string Content { get; init; }

    public required DateTime SentAt { get; init; }

    public required MessageStatus Status { get; init; }

    public required int? MessageIndex { get; init; }

    public required Guid? InReplyToMessageId { get; init; }

    public required int? VariantIndex { get; init; }

    public required bool IsSelectedVariant { get; init; }

    public ResolvedChatVariantNavigation? VariantNavigation { get; init; }

    public string? GenerationErrors { get; set; }

    public IReadOnlyList<ResolvedCitation> Citations { get; init; } = [];
}
