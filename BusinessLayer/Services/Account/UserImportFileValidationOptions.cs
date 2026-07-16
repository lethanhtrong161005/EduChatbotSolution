using System.Collections.Immutable;

namespace Business.Services.Account;

public sealed class UserImportFileValidationOptions
{
    public ImmutableHashSet<string> AllowedExtensions { get; set; } = [];

    public ImmutableHashSet<string> AllowedMimeType { get; set; } = [];
}
