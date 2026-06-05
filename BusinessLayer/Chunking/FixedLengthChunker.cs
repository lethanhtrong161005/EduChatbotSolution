using Domain.Contracts;
using Domain.DTOs;
using Domain.Entities;

namespace Business.Chunking;

public class FixedLengthChunker(
    int chunkSize = 1000,
    int overlap = 200)
    : IDocumentChunker
{
    private readonly int _chunkSize = chunkSize;
    private readonly int _overlap = overlap;

    public string ChunkStrategy => "FixedLength";

    public IEnumerable<ChunkDto> Chunk(params IEnumerable<ParsedSection> sections)
    {
        var chunkIndex = 0;

        foreach (var section in sections)
        {
            var text = section.Text;

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var start = 0;

            while (start < text.Length)
            {
                var length = Math.Min(_chunkSize, text.Length - start);

                yield return new ChunkDto
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
}
