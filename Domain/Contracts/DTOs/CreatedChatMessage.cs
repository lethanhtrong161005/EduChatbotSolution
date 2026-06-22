using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record CreatedChatMessage
{
    public required Guid Id { get; init; }

    public required ChatRole ChatRole { get; init; }

    public required string Content { get; init; }

    public IReadOnlyList<ResolvedCitation> Citations { get; init; } = [];

    public required DateTime SentAt { get; init; }
}
