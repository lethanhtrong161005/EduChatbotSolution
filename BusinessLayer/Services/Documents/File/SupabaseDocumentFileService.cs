using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.Extensions.Options;
using Supabase.Storage;

namespace Business.Services.Documents.File;

public class SupabaseDocumentFileService(
    IDocumentService documentService,
    Supabase.Client supabase,
    IOptions<SupabaseOptions> supabaseOpts,
    IOptions<FileStorageOptions> storageOpts,
    ITemporaryStorageService tempStorageService)
    : IDocumentFileService
{
    private readonly IDocumentService _documentService = documentService;
    private readonly Supabase.Client _supabase = supabase;
    private readonly SupabaseOptions _supabaseOpts = supabaseOpts.Value;
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;
    private readonly ITemporaryStorageService _tempStorageService = tempStorageService;

    public async Task<bool> Exists(Guid docId, CancellationToken cxlTkn = default)
    {
        var doc = await _documentService.GetByIdAsync(docId, cancellationToken: cxlTkn);
        if (doc == null) return false;

        var filePath = doc.FilePath;
        var prefix = Path.GetDirectoryName(filePath) ?? string.Empty;

        var files = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).List(path: prefix, options: new SearchOptions
        {
            Search = doc.FileName,
        });

        return files?.FirstOrDefault() != null;
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

        var supabaseDir = GetPhysicalDirectory(DocumentFileDirectory.Uploaded);
        var supabasePath = Path.Combine(supabaseDir, doc.FileName);

        var fullSupabasePath = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Upload(doc.FilePath, supabasePath);

        if (string.IsNullOrEmpty(fullSupabasePath))
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Failed to upload to Supabase bucket '{_supabaseOpts.DocumentBucket}'."],
            };
        }

        if (fullSupabasePath != _supabaseOpts.DocumentBucket + "/" + supabasePath)
        {
            // Log WARN
            supabasePath = fullSupabasePath[(fullSupabasePath.IndexOf('/') + 1)..];
        }

        doc.FilePath = supabasePath;
        await _documentService.UpdateAsync(doc, cxlTkn);

        return new FileOperationResult
        {
            Success = true,
            FilePath = supabasePath,
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

        var localPath = _tempStorageService.GetTempFilePath(doc.FileName);

        var actualLocalPath = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Download(doc.FilePath, localPath, onProgress: null);

        if (string.IsNullOrWhiteSpace(actualLocalPath))
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Failed to download from Supabase bucket '{_supabaseOpts.DocumentBucket}'."],
            };
        }

        return new FileOperationResult
        {
            Success = true,
            FilePath = actualLocalPath,
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

        var newSupabaseDir = GetPhysicalDirectory(newDirectory);
        var newSupabasePath = Path.Combine(newSupabaseDir, doc.FileName);

        var success = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Move(doc.FilePath, newSupabasePath);

        if (!success)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Failed to move file on Supabase bucket '{_supabaseOpts.DocumentBucket}'."],
            };
        }

        doc.FilePath = newSupabasePath;
        await _documentService.UpdateAsync(doc, cxlTkn);

        return new FileOperationResult
        {
            Success = true,
            FilePath = newSupabasePath,
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

        var result = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Remove(doc.FilePath);

        if (result == null)
        {
            return new FileOperationResult
            {
                Success = false,
                Errors = [$"Failed to delete file from Supabase bucket '{_supabaseOpts.DocumentBucket}'."],
            };
        }

        doc.FilePath = string.Empty;
        await _documentService.UpdateAsync(doc, cxlTkn);

        return new FileOperationResult { Success = true };
    }

    private string GetPhysicalDirectory(DocumentFileDirectory dir) => dir switch
    {
        DocumentFileDirectory.Uploaded => _storageOpts.FileDirectoryUploaded,
        DocumentFileDirectory.Processing => _storageOpts.FileDirectoryProcessing,
        DocumentFileDirectory.Indexed => _storageOpts.FileDirectoryIndexed,
        DocumentFileDirectory.Failed => _storageOpts.FileDirectoryFailed,
        _ => throw new ArgumentException("Should never be reached.", nameof(dir)),
    };
}
