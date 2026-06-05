using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Contracts;
using Domain.DTOs;
using Domain.Entities;
using System.Text;
using UglyToad.PdfPig;

using Ent = Domain.Entities;

namespace Business.Parsing;

public class SimpleParser : IDocumentParser
{
    public string ParserName => "SimpleParser";

    public async Task<ParsedDocument> ParseAsync(string path, Ent.DocumentType type)
    {
        return type switch
        {
            Ent.DocumentType.TXT =>
                await ParseTxtAsync(path),

            Ent.DocumentType.PDF =>
                await ParsePdfAsync(path),

            Ent.DocumentType.DOCX =>
                await ParseDocxAsync(path),

            Ent.DocumentType.PPTX =>
                await ParsePptxAsync(path),

            _ => throw new NotSupportedException()
        };
    }

    private async Task<ParsedDocument> ParseTxtAsync(string path)
    {
        var section = new ParsedSection
        {
            PageNumber = null,
            SectionTitle = null,
            Text = await File.ReadAllTextAsync(path),
        };
        return new ParsedDocument
        {
            Sections = [section],
        };
    }


    private async Task<ParsedDocument> ParsePdfAsync(string path)
    {
        using var pdf = PdfDocument.Open(path);

        var parsedDoc = new ParsedDocument();

        foreach (var page in pdf.GetPages())
        {
            parsedDoc.Sections.Add(new ParsedSection
            {
                PageNumber = page.Number,
                SectionTitle = null,
                Text = page.Text,
            });
        }

        return parsedDoc;
    }

    private async Task<ParsedDocument> ParseDocxAsync(string path)
    {
        using var wordDoc = WordprocessingDocument.Open(path, false);

        var parsedDoc = new ParsedDocument();

        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body == null)
            return parsedDoc;

        var headingPath = new Dictionary<int, string>();
        var sectionText = new StringBuilder();
        bool hasBodyText = false;

        foreach (var para in body.Elements<Paragraph>())
        {
            // Check if heading
            // NO: Read text.
            // YES: Flush section (if not only headings). Update heading path. Read text.

            var text = para.InnerText;
            var headingLvl = GetHeadingLevel(para);

            if (headingLvl == null)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                sectionText.AppendLine(text);
                hasBodyText = true;
            }
            else
            {
                if (hasBodyText)
                {
                    FlushCurrentSection();
                }

                text = !string.IsNullOrWhiteSpace(text) ? text : "_";

                headingPath[headingLvl.Value] = text;

                var deeperLvls = headingPath.Keys.Where(x => x > headingLvl);
                foreach (var lvl in deeperLvls)
                {
                    headingPath.Remove(lvl);
                }

                sectionText.AppendLine(text);
            }
        }

        if (sectionText.Length > 0)
        {
            FlushCurrentSection();
        }

        int? GetHeadingLevel(Paragraph para)
        {
            var paraStyle = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

            if (string.IsNullOrWhiteSpace(paraStyle)
                || !paraStyle.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var levelTxt = paraStyle["Heading".Length..];

            return int.TryParse(levelTxt, out var level)
                ? level
                : null;
        }

        void FlushCurrentSection()
        {
            var headings = headingPath.OrderBy(x => x.Key).Select(x => x.Value);

            var sectionTitle = string.Join(" > ", headings);

            parsedDoc.Sections.Add(new ParsedSection
            {
                PageNumber = null,
                SectionTitle = sectionTitle,
                Text = sectionText.ToString(),
            });

            sectionText.Clear();
            hasBodyText = false;
        }

        return parsedDoc;
    }

    private async Task<ParsedDocument> ParsePptxAsync(string path)
    {
        return null!;
    }
}
