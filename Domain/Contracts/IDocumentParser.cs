using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentParser
{
    string ParserName { get; }

    Task<ParsedDocument> ParseAsync(Stream source, DocumentType type, CancellationToken cancellationToken = default);
}
