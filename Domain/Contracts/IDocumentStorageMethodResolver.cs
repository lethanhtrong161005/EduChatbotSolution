using Domain.Entities;
using Domain.Utils;

namespace Domain.Contracts;

public interface IDocumentStorageMethodResolver
{
    Task<FileStorageMethod> ResolveForPersistenceAsync(
        Document document,
        CancellationToken cancellationToken = default);
}
