namespace Domain.Contracts.DTOs;

public record ChatSessionInfo
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required int? SubjectId { get; init; }

    public required string Title { get; init; }

    public required DateTime LastMessageAt { get; init; }

    public required int MessageCount { get; init; }
}
