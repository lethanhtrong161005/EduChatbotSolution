namespace Presentation.DTOs;

public sealed class SubjectSummaryDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ChapterCount { get; set; }

    public int DocumentCount { get; set; }

    public int MemberCount { get; set; }

    public bool IsChief { get; set; }
}

public sealed class ChapterSummaryDto
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int ChapterNumber { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class SubjectDetailsDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ChapterCount { get; set; }

    public int DocumentCount { get; set; }

    public int MemberCount { get; set; }

    public bool IsChief { get; set; }

    public DateTime LastUpdated { get; set; }

    public IReadOnlyList<ChapterSummaryDto> Chapters { get; set; } = [];
}

public sealed class ChapterDetailsDto
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int? ChapterNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DocumentCount { get; set; }
}

public sealed class DocumentFileDto
{
    public Guid Id { get; set; }

    public int SubjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string UploadedBy { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? FileSize { get; set; }

    public IReadOnlyList<ChapterSummaryDto> Chapters { get; set; } = [];
}
