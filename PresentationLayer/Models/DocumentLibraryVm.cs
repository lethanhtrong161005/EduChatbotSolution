namespace Presentation.Models;

public class DocumentLibraryVm
{
    public List<SubjectLookupVm> Subjects { get; set; } = [];
}

public class SubjectLookupVm
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class ChapterLookupVm
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class DocumentFileVm
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
