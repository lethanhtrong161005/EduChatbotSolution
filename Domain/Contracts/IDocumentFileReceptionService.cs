using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentFileReceptionService
{
    Task<FileReceptionResult> ReceiveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
