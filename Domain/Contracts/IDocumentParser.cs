using Domain.Contracts.DTOs;
using Domain.Utils;

namespace Domain.Contracts;

public interface IDocumentParser
{
    string ParserName { get; }

    Task<ParsedDocument> ParseAsync(Stream source, FileType type, CancellationToken cancellationToken = default);
}
