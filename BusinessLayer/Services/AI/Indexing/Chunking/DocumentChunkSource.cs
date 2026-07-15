using Domain.Entities;
using System.Text;

namespace Business.Services.AI.Indexing.Chunking;

internal sealed class DocumentChunkSource
{
    public const string SectionSeparator = "\n";

    private readonly SourceSpan[] _spans;

    public string Text { get; }

    private DocumentChunkSource(string text, SourceSpan[] spans)
    {
        Text = text;
        _spans = spans;
    }

    public static DocumentChunkSource Create(IReadOnlyList<ParsedSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);

        var text = new StringBuilder();
        var spans = new List<SourceSpan>();

        foreach (var section in sections.OrderBy(e => e.SectionIndex))
        {
            if (string.IsNullOrWhiteSpace(section.Text)) continue;
            if (text.Length > 0) text.Append(SectionSeparator);

            var start = text.Length;
            text.Append(section.Text);
            spans.Add(new SourceSpan(start, text.Length, section.PageNumber, section.SectionTitle));
        }

        return new DocumentChunkSource(text.ToString(), [.. spans]);
    }

    public ChunkLocation Resolve(int start, int length)
    {
        var end = start + length;
        SourceSpan? first = null;
        SourceSpan? last = null;

        foreach (var span in _spans)
        {
            if (span.Start >= end || span.EndExclusive <= start) continue;
            first ??= span;
            last = span;
        }

        return first is null
            ? default
            : new ChunkLocation(first.Value.PageNumber, last!.Value.PageNumber, first.Value.SectionTitle, last.Value.SectionTitle);
    }

    private readonly record struct SourceSpan(int Start, int EndExclusive, int? PageNumber, string? SectionTitle);
}

internal readonly record struct ChunkLocation(int? StartPageNumber, int? EndPageNumber, string? StartSectionTitle, string? EndSectionTitle);
