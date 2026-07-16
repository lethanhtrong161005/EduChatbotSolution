using System.Collections.Immutable;

namespace Business.Services.Documents.File;

public sealed class DocumentFileValidationOptions
{
    public ImmutableHashSet<string> AllowedExtensions { get; set; } = [];

    public ImmutableHashSet<string> AllowedMimeType { get; set; } = [];
}
