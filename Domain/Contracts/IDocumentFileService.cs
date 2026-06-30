using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentFileService
{
    Task<bool> Exists(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileOperationResult> Upload(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileOperationResult> Download(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileOperationResult> Move(Guid documentId, DocumentFileDirectory newDirectory, CancellationToken cancellationToken = default);

    Task<FileOperationResult> Delete(Guid documentId, CancellationToken cancellationToken = default);
}

public enum DocumentFileDirectory
{
    Uploaded,
    Processing,
    Indexed,
    Failed,
}
