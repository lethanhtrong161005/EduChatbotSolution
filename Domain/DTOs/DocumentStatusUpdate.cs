using Domain.Entities;

namespace Domain.DTOs;

public record DocumentStatusUpdate
{
    public required Guid Id { get; set; }

    public required DocumentStatus Status { get; set; }

    public double? Progress { get; set; }

    public string? ParserUsed { get; set; }

    public int? ChunkCount { get; set; }

    public string? EmbeddingModel { get; set; }

    public DateTime UpdatedAt { get; set; }
}
