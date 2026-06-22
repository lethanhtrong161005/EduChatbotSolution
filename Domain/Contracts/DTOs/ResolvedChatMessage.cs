using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record ResolvedChatMessage
{
    public required Guid Id { get; init; }

    public required ChatRole ChatRole { get; init; }

    public required string Content { get; init; }

    public required DateTime SentAt { get; init; }

    public required MessageStatus Status { get; init; }

    public string? GenerationErrors { get; set; }

    public IReadOnlyList<ResolvedCitation> Citations { get; init; } = [];
}
