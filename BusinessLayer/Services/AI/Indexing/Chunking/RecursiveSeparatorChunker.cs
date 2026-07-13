using Domain.Constants;
using Domain.Contracts.DTOs;

namespace Business.Services.AI.Indexing.Chunking;

public sealed class RecursiveSeparatorChunker : DocumentChunkerBase
{
    private static readonly string[] LineSeparators = ["\r\n\r\n", "\n\n", "\r\n", "\n"];

    public override string StrategyName => ChunkingStrategy.RecursiveSeparator;

    protected override IEnumerable<TextRange> Split(string text, ChunkingOptions options)
    {
        for (var start = 0; start < text.Length;)
        {
            var maxEnd = Math.Min(start + options.ChunkSize, text.Length);
            if (maxEnd == text.Length)
            {
                yield return new TextRange(start, maxEnd - start);
                yield break;
            }

            var split = FindSplit(text, start, maxEnd);
            yield return new TextRange(start, split.End - start);
            start = options.ChunkOverlap == 0 ? split.NextStart : Math.Max(start + 1, split.End - options.ChunkOverlap);
        }
    }

    private static SplitPoint FindSplit(string text, int start, int maxEnd)
    {
        foreach (var separator in LineSeparators)
        {
            if (IsAt(text, maxEnd, separator)) return new SplitPoint(maxEnd, SkipWhitespace(text, maxEnd));

            var index = LastIndexOf(text, separator, start, maxEnd);
            if (index > start) return new SplitPoint(index, SkipWhitespace(text, index + separator.Length));
        }

        for (var i = maxEnd - 1; i >= start; i--)
            if (IsSentenceEnd(text[i])) return new SplitPoint(i + 1, SkipWhitespace(text, i + 1));

        for (var i = maxEnd - 1; i > start; i--)
            if (char.IsWhiteSpace(text[i])) return new SplitPoint(i, SkipWhitespace(text, i));

        return new SplitPoint(maxEnd, maxEnd);
    }

    private static int LastIndexOf(string text, string value, int start, int endExclusive)
    {
        for (var i = endExclusive - value.Length; i >= start; i--)
            if (text.AsSpan(i, value.Length).SequenceEqual(value)) return i;

        return -1;
    }

    private static bool IsAt(string text, int index, string value) => index >= 0 && index + value.Length <= text.Length && text.AsSpan(index, value.Length).SequenceEqual(value);

    private static bool IsSentenceEnd(char value) => value is '.' or '!' or '?' or '…';

    private static int SkipWhitespace(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        return index;
    }

    private readonly record struct SplitPoint(int End, int NextStart);
}
