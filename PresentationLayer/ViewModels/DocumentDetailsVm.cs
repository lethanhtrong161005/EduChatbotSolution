namespace Presentation.ViewModels;

public sealed class DocumentDetailsVm
{
    public Guid Id { get; set; }

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long? FileSize { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ParserUsed { get; set; }

    public string? EmbeddingModel { get; set; }

    public int? ChunkCount { get; set; }

    public string? IndexingErrors { get; set; }

    public DateTime UploadedAt { get; set; }

    public Guid UploaderId { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    public string? ExtractedText { get; set; }

    public List<ChapterInfoVm> Chapters { get; set; } = [];

    public List<ParsedSectionVm> ParsedSections { get; set; } = [];

    public List<DocumentCommentVm> Comments { get; set; } = [];
}

public sealed class ChapterInfoVm
{
    public int Id { get; set; }

    public int ChapterNumber { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class ParsedSectionVm
{
    public int SectionIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? LocationInDocument { get; set; }
}

public sealed class DocumentCommentVm
{
    public Guid AuthorId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string AuthorRole { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
