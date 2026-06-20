namespace Domain.Contracts.DTOs;

public record ChatSessionHeader
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTime LastMessageAt { get; init; }
}
