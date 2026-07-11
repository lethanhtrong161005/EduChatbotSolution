namespace Domain.Contracts.DTOs;

public record FileDeletionResult
{
    public required bool Success { get; init; }

    public string[] Errors { get; init; } = [];
}
