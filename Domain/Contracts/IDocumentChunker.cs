using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentChunker
{
    string StrategyName { get; }

    IReadOnlyList<ChunkResult> Chunk(IReadOnlyList<ParsedSection> sections, ChunkingOptions options, int startIndex = 0);
}
