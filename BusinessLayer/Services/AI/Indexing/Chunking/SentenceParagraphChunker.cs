using Domain.Constants;
using Domain.Contracts.DTOs;

namespace Business.Services.AI.Indexing.Chunking;

public sealed class SentenceParagraphChunker : DocumentChunkerBase
{
    public override string StrategyName => ChunkingStrategy.SentenceParagraph;

    protected override IEnumerable<TextRange> Split(string text, ChunkingOptions options)
    {
        if (text.Length <= options.ChunkSize)
        {
            yield return new TextRange(0, text.Length);
            yield break;
        }

        var units = ReadUnits(text);

        for (var i = 0; i < units.Count;)
        {
            var first = units[i];

            if (first.Length > options.ChunkSize)
            {
                foreach (var range in FixedRanges(first.Start, first.Length, options)) yield return range;
                i++;
                continue;
            }

            var j = i + 1;
            while (j < units.Count && units[j].EndExclusive - first.Start <= options.ChunkSize) j++;

            yield return new TextRange(first.Start, units[j - 1].EndExclusive - first.Start);
            if (j == units.Count) yield break;

            var next = j;

            if (options.ChunkOverlap > 0 && j - i > 1)
            {
                next = j - 1;
                while (next > i + 1 && units[j - 1].EndExclusive - units[next - 1].Start <= options.ChunkOverlap) next--;
                while (next < j && units[j].EndExclusive - units[next].Start > options.ChunkSize) next++;
                if (next >= j) next = j;
            }

            i = next;
        }
    }

    private static List<SentenceUnit> ReadUnits(string text)
    {
        var units = new List<SentenceUnit>();
        var start = SkipWhitespace(text, 0);

        for (var i = start; i < text.Length; i++)
        {
            if (IsSentenceEnd(text[i]))
            {
                var end = i + 1;
                while (end < text.Length && IsSentenceEnd(text[end])) end++;
                while (end < text.Length && IsClosingCharacter(text[end])) end++;

                units.Add(new SentenceUnit(start, end));
                start = SkipWhitespace(text, end);
                i = start - 1;
                continue;
            }

            if (!IsParagraphBreak(text, i)) continue;

            if (i > start) units.Add(new SentenceUnit(start, i));
            start = SkipWhitespace(text, i + 1);
            i = start - 1;
        }

        if (start < text.Length) units.Add(new SentenceUnit(start, text.Length));
        return units;
    }

    private static bool IsParagraphBreak(string text, int index)
    {
        if (text[index] != '\n') return false;
        var next = index + 1;
        if (next < text.Length && text[next] == '\r') next++;
        return next < text.Length && text[next] == '\n';
    }

    private static bool IsSentenceEnd(char value) => value is '.' or '!' or '?' or '…';

    private static bool IsClosingCharacter(char value) => value is '"' or '\'' or '”' or '’' or ')' or ']' or '}';

    private static int SkipWhitespace(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        return index;
    }

    private readonly record struct SentenceUnit(int Start, int EndExclusive)
    {
        public int Length => EndExclusive - Start;
    }
}
