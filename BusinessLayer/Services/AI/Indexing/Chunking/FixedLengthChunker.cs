using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Business.Services.AI.Indexing.Chunking;

public class FixedLengthChunker(
    int chunkSize = 1000,
    int overlap = 200)
    : IDocumentChunker
{
    private readonly int _chunkSize = chunkSize;
    private readonly int _overlap = overlap;

    public IEnumerable<ChunkResult> Chunk(ParsedSection section, int startIndex = 0)
    {
        var chunkIndex = startIndex;
        var text = section.Text;

        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var start = 0;

        while (start < text.Length)
        {
            var length = Math.Min(_chunkSize, text.Length - start);

            yield return new ChunkResult
            {
                ChunkIndex = chunkIndex++,
                ChunkText = text.Substring(start, length),
                PageNumber = section.PageNumber,
                SectionTitle = section.SectionTitle,
            };

            if (start + length >= text.Length)
                break;

            start += _chunkSize - _overlap;
        }
    }
}
