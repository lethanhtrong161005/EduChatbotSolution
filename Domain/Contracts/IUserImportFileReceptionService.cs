using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IUserImportFileReceptionService
{
    Task<FileReceptionResult> ReceiveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken);
}
