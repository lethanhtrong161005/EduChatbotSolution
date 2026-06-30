using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface ITemporaryStorageService
{
    Task<FileInspectionResult> ValidateAndSave(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    string GetTempFilePath(string? fileName = null);
}
