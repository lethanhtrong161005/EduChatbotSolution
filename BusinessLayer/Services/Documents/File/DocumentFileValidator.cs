using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Utils;
using Microsoft.Extensions.Options;
using MimeDetective;
using System.Collections.Immutable;

namespace Business.Services.Documents.File;

public sealed class DocumentFileValidator(
    IContentInspector inspector,
    IOptions<FileValidationOptions> validationOpts)
    : IDocumentFileValidator
{
    private readonly IContentInspector _inspector = inspector;
    private readonly FileValidationOptions _validationOpts = validationOpts.Value;

    public Task<FileValidationResult> ValidateAsync(
        Stream content,
        string originalFileName,
        CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(originalFileName);
        var normalizedExtension = extension.TrimStart('.');
        var inspection = _inspector.Inspect(content);

        var detectedExtensions = inspection
            .ByFileExtension()
            .Select(e => e.Extension.TrimStart('.'))
            .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);
        var detectedMimeTypes = inspection
            .ByMimeType()
            .Select(e => e.MimeType)
            .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

        var valid = detectedExtensions.Contains(normalizedExtension)
                    && _validationOpts.AllowedExtensions.Contains(normalizedExtension)
                    && _validationOpts.AllowedMimeType.Overlaps(detectedMimeTypes);

        return Task.FromResult(valid
            ? new FileValidationResult
            {
                Success = true,
                FileType = FileHelper.ParseFileType(extension),
            }
            : new FileValidationResult
            {
                Success = false,
                Errors = ["Unsupported file type."],
            });
    }
}
