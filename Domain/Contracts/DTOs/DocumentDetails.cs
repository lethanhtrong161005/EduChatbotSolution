using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record DocumentDetails
{
    public required Guid Id { get; init; }

    public required int SubjectId { get; init; }

    public required string SubjectName { get; init; } = string.Empty;

    public required string Title { get; init; } = string.Empty;

    public required string? Description { get; init; } = string.Empty;

    public required string OriginalFileName { get; init; } = string.Empty;

    public required DocumentType FileType { get; init; }

    public required long? FileSize { get; init; }

    public required string Status { get; init; } = string.Empty;

    public required string? ParserUsed { get; init; }

    public required string? EmbeddingModel { get; init; }

    public required int ChunkCount { get; init; }

    public required string? IndexingErrors { get; init; }

    public required DateTime UploadedAt { get; init; }

    public required Guid UploaderId { get; init; }

    public required string UploadedBy { get; init; } = string.Empty;

    public List<ChapterInfo> Chapters { get; set; } = [];

    public List<ParsedSectionDetails> ParsedSections { get; set; } = [];

    public List<DocumentCommentDetails> Comments { get; set; } = [];
}

public record ChapterInfo
{
    public required int Id { get; init; }

    public required int ChapterNumber { get; init; }

    public required string Name { get; init; } = string.Empty;
}

public record ParsedSectionDetails
{
    public required int SectionIndex { get; init; }

    public required string Text { get; init; } = string.Empty;

    public required string? LocationInDocument { get; init; }
}

public record DocumentCommentDetails
{
    public required Guid AuthorId { get; init; }

    public required string AuthorName { get; init; } = string.Empty;

    public required string AuthorRole { get; set; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public required DateTime CreatedAt { get; init; }
}
