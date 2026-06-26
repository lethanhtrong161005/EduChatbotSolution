using Domain.Entities;

namespace Presentation.ViewModels;

public class DocumentDetailsVm
{
    public Guid Id { get; set; }

    public int SubjectId { get; set; }

    public int ChapterId { get; set; }

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

    public Guid UploaderId { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    public string? ExtractedText { get; set; }

    public List<ParsedSectionVm> ParsedSections { get; set; } = [];

    public List<DocumentComment> Comments { get; set; } = [];
}
