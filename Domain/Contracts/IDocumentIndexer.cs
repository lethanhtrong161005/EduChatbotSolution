namespace Domain.Contracts;

public interface IDocumentIndexer
{
    Task ParseAsync(Guid documentId);
    Task ChunkAsync(Guid documentId);
    Task EmbedAsync(Guid documentId);
}
