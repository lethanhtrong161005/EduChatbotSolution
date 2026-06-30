using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Business.Services.Documents.File;

public class LocalHardDriveDocumentFileService(
    IDocumentService documentService,
    IOptions<FileStorageOptions> storageOpts)
    : IDocumentFileService
{
    private readonly IDocumentService _documentService = documentService;
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;

    public async Task<bool> Exists(Guid docId, CancellationToken cxlTkn = default)
    {
        var doc = await _documentService.GetByIdAsync(docId, cancellationToken: cxlTkn);
        if (doc == null) return false;
        return Exists(doc);
    }

    public async Task<FileOperationResult> Upload(Guid docId, CancellationToken cxlTkn = default)
    {
        var doc = await _documentService.GetByIdAsync(docId, cancellationToken: cxlTkn);
        if (doc == null)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"No document matched the provided ID."],
            };
        }

        if (!Exists(doc))
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Document file not found at '{doc.FilePath}'."],
            };
        }

        var uploadDir = GetPhysicalDirectory(DocumentFileDirectory.Uploaded);
        var newPath = Move(doc, uploadDir);

        doc.FilePath = newPath;
        await _documentService.UpdateAsync(doc, cxlTkn);

        return new FileOperationResult
        {
            Success = true,
            FilePath = newPath,
        };
    }

    public async Task<FileOperationResult> Download(Guid docId, CancellationToken cxlTkn = default)
    {
        var doc = await _documentService.GetByIdAsync(docId, cancellationToken: cxlTkn);
        if (doc == null)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"No document matched the provided ID."],
            };
        }

        if (!Exists(doc))
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Document file not found at '{doc.FilePath}'."],
            };
        }

        return new FileOperationResult
        {
            Success = true,
            FilePath = doc.FilePath,
        };
    }

    public async Task<FileOperationResult> Move(Guid docId, DocumentFileDirectory newDirectory, CancellationToken cxlTkn = default)
    {
        var doc = await _documentService.GetByIdAsync(docId, cancellationToken: cxlTkn);
        if (doc == null)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"No document matched the provided ID."],
            };
        }

        if (!Exists(doc))
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Document file not found at '{doc.FilePath}'."],
            };
        }

        var newDir = GetPhysicalDirectory(newDirectory);
        var newPath = Move(doc, newDir);

        doc.FilePath = newPath;
        await _documentService.UpdateAsync(doc, cxlTkn);

        return new FileOperationResult
        {
            Success = true,
            FilePath = newPath,
        };
    }

    public async Task<FileOperationResult> Delete(Guid docId, CancellationToken cxlTkn = default)
    {
        var doc = await _documentService.GetByIdAsync(docId, cancellationToken: cxlTkn);
        if (doc == null)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"No document matched the provided ID."],
            };
        }

        try
        {
            System.IO.File.Delete(doc.FilePath);
            doc.FilePath = string.Empty;
            await _documentService.UpdateAsync(doc, cxlTkn);
            return new FileOperationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [ex.ToString()],
            };
        }
    }

    private static bool Exists(Document doc) => System.IO.File.Exists(doc.FilePath);

    private static string Move(Document doc, string newDirectory)
    {
        var newPath = Path.Combine(newDirectory, doc.FileName);
        Directory.CreateDirectory(newDirectory);
        System.IO.File.Move(doc.FilePath, newPath);
        return newPath;
    }

    private static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

    private string GetPhysicalDirectory(DocumentFileDirectory dir) => dir switch
    {
        DocumentFileDirectory.Uploaded => Path.Combine(AppData, _storageOpts.AppDirectory, _storageOpts.FileDirectoryUploaded),
        DocumentFileDirectory.Processing => Path.Combine(AppData, _storageOpts.AppDirectory, _storageOpts.FileDirectoryProcessing),
        DocumentFileDirectory.Indexed => Path.Combine(AppData, _storageOpts.AppDirectory, _storageOpts.FileDirectoryIndexed),
        DocumentFileDirectory.Failed => Path.Combine(AppData, _storageOpts.AppDirectory, _storageOpts.FileDirectoryFailed),
        _ => throw new ArgumentException("Should never be reached.", nameof(dir)),
    };
}
