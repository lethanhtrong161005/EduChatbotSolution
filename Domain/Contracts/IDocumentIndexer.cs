namespace Domain.Contracts;

public interface IDocumentIndexer
{
    Task ParseAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task ChunkAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task EmbedAsync(Guid documentId, CancellationToken cancellationToken = default);
}
