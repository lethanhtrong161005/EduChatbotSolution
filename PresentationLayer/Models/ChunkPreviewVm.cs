namespace Presentation.Models;

public class ChunkPreviewPageVm
{
    public List<ChunkPreviewVm> Chunks { get; set; } = [];

    public int PageIndex { get; set; }

    public int TotalPages { get; set; }
}

public sealed class ChunkPreviewVm
{
    public Guid Id { get; set; }

    public int ChunkIndex { get; set; }

    public string ChunkText { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public string? SectionTitle { get; set; }

    public int? TokenCount { get; set; }

    public string? EmbeddingModel { get; set; }

    public float[] VectorPreview { get; set; } = [];
}
