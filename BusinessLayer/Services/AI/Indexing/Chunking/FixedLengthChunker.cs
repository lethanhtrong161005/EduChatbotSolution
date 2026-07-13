using Domain.Constants;
using Domain.Contracts.DTOs;

namespace Business.Services.AI.Indexing.Chunking;

public sealed class FixedLengthChunker : DocumentChunkerBase
{
    public override string StrategyName => ChunkingStrategy.FixedLength;

    protected override IEnumerable<TextRange> Split(string text, ChunkingOptions options) => FixedRanges(0, text.Length, options);
}
