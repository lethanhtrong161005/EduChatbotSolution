namespace Presentation.DTOs;

public class ChunkPreviewPageDto
{
    public List<ChunkPreviewDto> Chunks { get; set; } = [];

    public int PageIndex { get; set; }

    public int TotalPages { get; set; }
}

public class ChunkPreviewDto
{
    public Guid Id { get; set; }

    public int ChunkIndex { get; set; }

    public string ChunkText { get; set; } = string.Empty;

    public int? StartPageNumber { get; set; }

    public int? EndPageNumber { get; set; }

    public string? StartSectionTitle { get; set; }

    public string? EndSectionTitle { get; set; }

    public int? TokenCount { get; set; }

    public string? EmbeddingModel { get; set; }

    public float[] VectorPreview { get; set; } = [];
}
