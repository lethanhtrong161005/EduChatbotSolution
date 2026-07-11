using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentStorageMethodResolver
{
    Task<DocumentStorageMethod> ResolveForPersistenceAsync(
        Document document,
        CancellationToken cancellationToken = default);
}
