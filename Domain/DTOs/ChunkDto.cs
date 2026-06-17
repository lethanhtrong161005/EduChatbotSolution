namespace Domain.DTOs;

public record ChunkDto
{
    public required int ChunkIndex { get; set; }

    public required string ChunkText { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public string? SectionTitle { get; set; }
}
