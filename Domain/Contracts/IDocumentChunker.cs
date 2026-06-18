using Domain.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentChunker
{
    string ChunkStrategy { get; }

    IEnumerable<ChunkingResult> Chunk(ParsedSection section);
}
