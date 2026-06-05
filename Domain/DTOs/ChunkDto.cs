namespace Domain.DTOs;

public class ChunkDto
{
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? SectionTitle { get; set; }
}
