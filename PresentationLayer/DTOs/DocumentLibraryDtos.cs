namespace Presentation.DTOs;

public class SubjectLookupDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class ChapterLookupDto
{
    public int Id { get; set; }

    public int? ChapterNumber { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class DocumentFileDto
{
    public Guid Id { get; set; }

    public int ChapterId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string UploadedBy { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public long? FileSize { get; set; }
}
