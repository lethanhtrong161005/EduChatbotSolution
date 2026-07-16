using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IUserImportFileService
{
    Task<FileStorageResult> PersistAsync(Guid batchId, CancellationToken cancellationToken);

    Task<FileReadResult> OpenReadAsync(Guid batchId, CancellationToken cancellationToken);
}
