namespace Domain.Contracts;

public interface IDocumentChunkerSelector
{
    IDocumentChunker Select(string strategy);
}
