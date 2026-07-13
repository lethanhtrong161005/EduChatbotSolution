using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record DocumentStatusUpdate
{
    public required Guid Id { get; init; }

    public required DocumentStatus Status { get; init; }

    public double? Progress { get; init; }

    public string? ParserUsed { get; init; }

    public string? ChunkingStrategy { get; init; }

    public int? ChunkCount { get; init; }

    public string? EmbeddingModel { get; init; }

    public required DateTime UpdatedAt { get; init; }
}
