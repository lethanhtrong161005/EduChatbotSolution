using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Utils;
using Microsoft.Extensions.Options;
using MimeDetective;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Business.Services.Documents.File;

public class TempPathStorageService(
    IContentInspector inspector,
    IOptions<FileValidationOptions> validationOpts,
    IOptions<FileStorageOptions> storageOpts)
    : ITemporaryStorageService
{
    private readonly IContentInspector _inspector = inspector;
    private readonly FileValidationOptions _validationOpts = validationOpts.Value;
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;

    public async Task<FileInspectionResult> ValidateAndSave(Stream fileStream, string fileName, CancellationToken cxlTkn = default)
    {
        var extension = Path.GetExtension(fileName);
        var storageName = $"{Guid.NewGuid()}{extension}";

        var tempDir = Path.Combine(Path.GetTempPath(), _storageOpts.AppDirectory, _storageOpts.FileDirectoryBuffer);
        Directory.CreateDirectory(tempDir);

        var fullPath = Path.Combine(tempDir, storageName);
        await using (var bufferFs = System.IO.File.Create(fullPath))
        {
            await fileStream.CopyToAsync(bufferFs, cxlTkn);
            if (fileStream.CanSeek) fileStream.Position = 0;
        }

        await using var inspectFs = System.IO.File.OpenRead(fullPath);
        var result = _inspector.Inspect(inspectFs);

        var fileExtensions = result
            .ByFileExtension()
            .Select(e => e.Extension)
            .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

        var mimeTypes = result
            .ByMimeType()
            .Select(e => e.MimeType)
            .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

        if (_validationOpts.AllowedExtensions.Intersect(fileExtensions).Count == 0
            || _validationOpts.AllowedMimeType.Intersect(mimeTypes).Count == 0)
        {
            System.IO.File.Delete(fullPath);
            return new FileInspectionResult
            {
                Success = false,
                Errors = ["File type not supported."],
            };
        }

        return new FileInspectionResult
        {
            Success = true,
            FilePath = fullPath,
            FileType = FileHelper.ParseFileType(extension),
        };
    }

    public string GetTempFilePath(string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = Path.GetRandomFileName();
        }

        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidCharRegex = string.Format(@"([{0}]+)|(\s+)", invalidChars);
        fileName = Regex.Replace(fileName, invalidCharRegex, "_");

        var tempDir = Path.Combine(Path.GetTempPath(), _storageOpts.AppDirectory, _storageOpts.FileDirectoryBuffer);
        Directory.CreateDirectory(tempDir);

        return Path.Combine(tempDir, fileName);
    }
}
