using Domain.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentParser
{
    string ParserName { get; }

    Task<ParsedDocument> ParseAsync(string path, DocumentType type, CancellationToken cancellationToken = default);
}
