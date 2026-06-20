using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentChunker
{
    IEnumerable<ChunkResult> Chunk(ParsedSection section, int startIndex = 0);
}
