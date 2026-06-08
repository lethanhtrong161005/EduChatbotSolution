using Domain.Entities;

namespace Presentation.Models;

public class DocumentDetailsVm
{
    public Guid Id { get; set; }

    public string ChapterName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long? FileSize { get; set; }

    public string Status { get; set; } = string.Empty;

    public double? Progress { get; set; }

    public string? ParserUsed { get; set; }

    public string? IndexingErrors { get; set; }

    public string? EmbeddingModel { get; init; }

    public int? ChunkCount { get; set; }

    public DateTime UploadedAt { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    public List<DocumentComment> Comments { get; set; } = [];
}
