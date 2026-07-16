using Business.Services.Storage;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Utils;
using Microsoft.Extensions.Options;
using MimeDetective;
using System.Collections.Immutable;

namespace Business.Services.Account;

public class UserImportFileReceptionService(
    IContentInspector inspector,
    IStagingFileStore stagingStore,
    ILocalFileBuffer localFileBuffer,
    IOptions<UserImportFileValidationOptions> validationOpts)
    : IUserImportFileReceptionService
{
    private readonly IContentInspector _inspector = inspector;
    private readonly IStagingFileStore _stagingStore = stagingStore;
    private readonly ILocalFileBuffer _localFileBuffer = localFileBuffer;
    private readonly UserImportFileValidationOptions _validationOpts = validationOpts.Value;

    public async Task<FileReceptionResult> ReceiveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cxlTkn = default)
    {
        try
        {
            return content.CanSeek
                ? await ReceiveSeekableAsync(content, originalFileName, cxlTkn)
                : await BufferAndReceiveAsync(content, originalFileName, cxlTkn);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Failure([ex.Message]);
        }
    }

    private async Task<FileReceptionResult> ReceiveSeekableAsync(
        Stream content,
        string originalFileName,
        CancellationToken cxlTkn)
    {
        var initialPosition = content.Position;
        try
        {
            var validation = Validate(content, originalFileName, cxlTkn);
            if (!validation.Success) return Failure(validation.Errors);

            content.Position = initialPosition;
            return await StageValidatedAsync(content, validation.FileType.Value, cxlTkn);
        }
        finally
        {
            content.Position = initialPosition;
        }
    }

    private async Task<FileReceptionResult> BufferAndReceiveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cxlTkn)
    {
        await using var lease = await _localFileBuffer.CopyFromAsync(content, Path.GetExtension(originalFileName), cxlTkn);

        await using var validationStream = lease.OpenRead();
        var validation = Validate(validationStream, originalFileName, cxlTkn);
        if (!validation.Success) return Failure(validation.Errors);

        await using var stagingStream = lease.OpenRead();
        return await StageValidatedAsync(stagingStream, validation.FileType.Value, cxlTkn);
    }

    private FileValidationResult Validate(Stream content, string originalFileName, CancellationToken cxlTkn)
    {
        cxlTkn.ThrowIfCancellationRequested();

        var initialPosition = content.Position;
        var extension = Path.GetExtension(originalFileName);
        var normalizedExtension = extension.TrimStart('.');

        var inspection = _inspector.Inspect(content);

        var detectedExtensions = inspection
            .ByFileExtension()
            .Select(x => x.Extension)
            .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);
        var detectedMimeTypes = inspection
            .ByMimeType()
            .Select(x => x.MimeType)
            .ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

        var valid = detectedExtensions.Contains(normalizedExtension)
            && _validationOpts.AllowedExtensions.Contains(normalizedExtension)
            && _validationOpts.AllowedMimeType.Overlaps(detectedMimeTypes);

        return valid
            ? new FileValidationResult
            {
                Success = true,
                FileType = FileHelper.ParseFileType(extension),
            }
            : new FileValidationResult
            {
                Success = false,
                Errors = [$"Unsupported file type. Must be one of: {string.Join(", ", $"'{string.Join(", ", _validationOpts.AllowedExtensions)}'")}."],
            };
    }

    private async Task<FileReceptionResult> StageValidatedAsync(
        Stream content,
        FileType fileType,
        CancellationToken cxlTkn)
    {
        var stagingResult = await _stagingStore.StageAsync(content, FileHelper.GetCanonicalExtension(fileType), cxlTkn);

        return stagingResult.Success
            ? new FileReceptionResult
            {
                Success = true,
                Locator = stagingResult.Locator,
                FileType = fileType,
            }
            : Failure(stagingResult.Errors);
    }

    private static FileReceptionResult Failure(string[] errors) =>
        new() { Success = false, Errors = errors };
}
