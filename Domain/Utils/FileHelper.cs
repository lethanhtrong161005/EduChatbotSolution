using Domain.Entities;

namespace Domain.Utils;

public static class FileHelper
{
    public static DocumentType ParseFileType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".txt" => DocumentType.TXT,
            ".docx" => DocumentType.DOCX,
            ".pdf" => DocumentType.PDF,
            ".html" => DocumentType.HTML,
            ".pptx" => DocumentType.PPTX,
            _ => DocumentType.Other,
        };

    public static string GetCanonicalExtension(DocumentType fileType) => fileType switch
    {
        DocumentType.TXT => ".txt",
        DocumentType.DOCX => ".docx",
        DocumentType.PDF => ".pdf",
        DocumentType.HTML => ".html",
        DocumentType.PPTX => ".pptx",
        _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unsupported document type."),
    };
}
