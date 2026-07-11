using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Utils;

namespace Business.Services.Documents.File;

public sealed class DocumentFileReceptionService(
    IDocumentFileValidator validator,
    IStagingFileStore stagingStore,
    ILocalFileBuffer localFileBuffer)
    : IDocumentFileReceptionService
{
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
            return Failure(ex.Message);
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
            var validation = await validator.ValidateAsync(content, originalFileName, cxlTkn);
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
        await using var lease = await localFileBuffer.CopyFromAsync(
            content,
            Path.GetExtension(originalFileName),
            cxlTkn);

        await using var validationStream = lease.OpenRead();
        var validation = await validator.ValidateAsync(validationStream, originalFileName, cxlTkn);
        if (!validation.Success) return Failure(validation.Errors);

        await using var stagingStream = lease.OpenRead();
        return await StageValidatedAsync(stagingStream, validation.FileType.Value, cxlTkn);
    }

    private async Task<FileReceptionResult> StageValidatedAsync(
        Stream content,
        Domain.Entities.DocumentType fileType,
        CancellationToken cxlTkn)
    {
        var stagingResult = await stagingStore.StageAsync(
            content,
            FileHelper.GetCanonicalExtension(fileType),
            cxlTkn);

        return stagingResult.Success
            ? new FileReceptionResult
            {
                Success = true,
                StagingLocator = stagingResult.Locator,
                FileType = fileType,
            }
            : Failure(stagingResult.Errors);
    }

    private static FileReceptionResult Failure(string error) =>
        new() { Success = false, Errors = [error] };

    private static FileReceptionResult Failure(string[] errors) =>
        new() { Success = false, Errors = errors };
}
