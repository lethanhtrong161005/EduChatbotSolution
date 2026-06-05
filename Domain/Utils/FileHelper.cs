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
            _ => DocumentType.Other
        };
}
