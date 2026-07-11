using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentFileValidator
{
    Task<FileValidationResult> ValidateAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
