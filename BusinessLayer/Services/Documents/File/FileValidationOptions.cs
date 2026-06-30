using System.Collections.Immutable;

namespace Business.Services.Documents.File;

public sealed class FileValidationOptions
{
    public ImmutableHashSet<string> AllowedExtensions { get; set; } = [];

    public ImmutableHashSet<string> AllowedMimeType { get; set; } = [];
}
