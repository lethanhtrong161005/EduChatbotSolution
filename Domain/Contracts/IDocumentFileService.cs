using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentFileService
{
    Task<bool> ExistsAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileStorageResult> PersistAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileReadResult> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileStorageResult> MoveAsync(Guid documentId, FileDirectoryCategory newDirectory, CancellationToken cancellationToken = default);

    Task<FileDeletionResult> DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public enum FileDirectoryCategory
{
    Received,
    Processing,
    Indexed,
    Failed,
}
