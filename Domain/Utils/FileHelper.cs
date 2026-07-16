namespace Domain.Utils;

public static class FileHelper
{
    public static FileType ParseFileType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".txt" => FileType.TXT,
            ".html" => FileType.HTML,
            ".pdf" => FileType.PDF,
            ".docx" => FileType.DOCX,
            ".xlsx" => FileType.XLSX,
            ".pptx" => FileType.PPTX,
            _ => FileType.Other,
        };

    public static string GetCanonicalExtension(FileType fileType) => fileType switch
    {
        FileType.TXT => ".txt",
        FileType.HTML => ".html",
        FileType.PDF => ".pdf",
        FileType.DOCX => ".docx",
        FileType.XLSX => ".xlsx",
        FileType.PPTX => ".pptx",
        _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unsupported document type."),
    };

    public static string GetMimeType(FileType fileType) => fileType switch
    {
        FileType.TXT => "text/plain",
        FileType.HTML => "text/html",
        FileType.PDF => "application/pdf",
        FileType.DOCX => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        FileType.XLSX => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        FileType.PPTX => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        _ => "application/octet-stream",
    };
}

public enum FileType
{
    TXT,
    HTML,
    PDF,
    DOCX,
    XLSX,
    PPTX,
    Other,
}

public enum FileStorageMethod
{
    Unspecified = 0,
    LocalHardDrive = 1,
    Supabase = 2,
}
