using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Business.Services.AI.Indexing.Chunking;

public abstract class DocumentChunkerBase : IDocumentChunker
{
    public abstract string StrategyName { get; }

    public IReadOnlyList<ChunkResult> Chunk(IReadOnlyList<ParsedSection> sections, ChunkingOptions options, int startIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var source = DocumentChunkSource.Create(sections);
        if (source.Text.Length == 0) return [];

        var results = new List<ChunkResult>();

        foreach (var range in Split(source.Text, options))
        {
            if (range.Start < 0 || range.Length <= 0 || range.EndExclusive > source.Text.Length)
                throw new InvalidOperationException($"Chunker '{StrategyName}' produced invalid range [{range.Start}, {range.EndExclusive}).");

            var text = source.Text.Substring(range.Start, range.Length);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var location = source.Resolve(range.Start, range.Length);
            results.Add(new ChunkResult
            {
                ChunkIndex = startIndex + results.Count,
                ChunkText = text,
                StartPageNumber = location.StartPageNumber,
                EndPageNumber = location.EndPageNumber,
                StartSectionTitle = location.StartSectionTitle,
                EndSectionTitle = location.EndSectionTitle,
            });
        }

        return results;
    }

    protected abstract IEnumerable<TextRange> Split(string text, ChunkingOptions options);

    protected static IEnumerable<TextRange> FixedRanges(int start, int length, ChunkingOptions options)
    {
        var end = start + length;

        while (start < end)
        {
            var currentLength = Math.Min(options.ChunkSize, end - start);
            yield return new TextRange(start, currentLength);
            if (start + currentLength >= end) yield break;
            start += options.ChunkSize - options.ChunkOverlap;
        }
    }

    protected readonly record struct TextRange(int Start, int Length)
    {
        public int EndExclusive => Start + Length;
    }
}
