using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using System.Text;
using UglyToad.PdfPig;

using Ent = Domain.Entities;

namespace Business.Services.AI.Indexing.Parsing;

public class LocationAnnotatedParser : IDocumentParser
{
    public string ParserName => "Location Annotated Parser";

    public async Task<ParsedDocument> ParseAsync(string path, Ent.DocumentType type, CancellationToken cxlTkn = default)
    {
        return type switch
        {
            Ent.DocumentType.TXT =>
                await ParseTxtAsync(path, cxlTkn),

            Ent.DocumentType.PDF =>
                await ParsePdfAsync(path, cxlTkn),

            Ent.DocumentType.DOCX =>
                await ParseDocxAsync(path, cxlTkn),

            Ent.DocumentType.PPTX =>
                await ParsePptxAsync(path, cxlTkn),

            _ => throw new NotSupportedException()
        };
    }

    private async Task<ParsedDocument> ParseTxtAsync(string path, CancellationToken cxlTkn = default)
    {
        var section = new ParsedSection
        {
            PageNumber = null,
            SectionTitle = null,
            Text = await File.ReadAllTextAsync(path, cxlTkn),
        };
        return new ParsedDocument { Sections = [section] };
    }

    private async Task<ParsedDocument> ParsePdfAsync(string path, CancellationToken cxlTkn = default)
    {
        using var pdf = PdfDocument.Open(path);

        var parsedDoc = new ParsedDocument { Sections = [] };

        int sectionIndex = 0;
        foreach (var page in pdf.GetPages())
        {
            cxlTkn.ThrowIfCancellationRequested();

            parsedDoc.Sections.Add(new ParsedSection
            {
                SectionIndex = sectionIndex++,
                PageNumber = page.Number,
                SectionTitle = null,
                Text = page.Text,
            });
        }

        return parsedDoc;
    }

    private async Task<ParsedDocument> ParseDocxAsync(string path, CancellationToken cxlTkn = default)
    {
        using var wordDoc = WordprocessingDocument.Open(path, false);

        var parsedDoc = new ParsedDocument { Sections = [] };

        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body == null)
            return parsedDoc;

        int sectionIndex = 0;
        var headingPath = new Dictionary<int, string>();
        var sectionText = new StringBuilder();
        bool hasBodyText = false;

        foreach (var para in body.Elements<Paragraph>())
        {
            cxlTkn.ThrowIfCancellationRequested();

            // Check if heading
            // NO: Read text.
            // YES: Flush section, if any body text. Update heading path. Read (heading) text.

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

        // Flush at end-of-file.
        // The check is meant to prevent flushing an empty section from a empty document.
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
                SectionIndex = sectionIndex++,
                PageNumber = null,
                SectionTitle = sectionTitle,
                Text = sectionText.ToString(),
            });

            sectionText.Clear();
            hasBodyText = false;
        }

        return parsedDoc;
    }

    private async Task<ParsedDocument> ParsePptxAsync(string path, CancellationToken cxlTkn = default)
    {
        var parsedDoc = new ParsedDocument { Sections = [] };
        using var ppt = PresentationDocument.Open(path, false);
        var presentationPart = ppt.PresentationPart;
        if (presentationPart == null) return parsedDoc;

        var slideIdList = presentationPart.Presentation?.SlideIdList;
        if (slideIdList == null) return parsedDoc;

        int slideIndex = 1;
        int sectionIndex = 0;
        foreach (var slideIdObj in slideIdList.Elements<DocumentFormat.OpenXml.Presentation.SlideId>())
        {
            cxlTkn.ThrowIfCancellationRequested();
            var slidePart = presentationPart.GetPartById(slideIdObj.RelationshipId!) as SlidePart;
            if (slidePart?.Slide == null) continue;

            var slideText = new StringBuilder();
            var slideTitle = $"Slide {slideIndex}";

            // Find all text elements on the slide
            var texts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var t in texts)
            {
                var text = t.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    slideText.AppendLine(text);
                }
            }

            // Attempt to find slide title placeholder text
            var titleShapes = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.Shape>()
                .Where(s => s.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape?.Type?.Value == DocumentFormat.OpenXml.Presentation.PlaceholderValues.Title
                         || s.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape?.Type?.Value == DocumentFormat.OpenXml.Presentation.PlaceholderValues.CenteredTitle);

            var titleText = titleShapes.FirstOrDefault()?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
            if (!string.IsNullOrWhiteSpace(titleText))
            {
                slideTitle = titleText.Trim();
            }

            parsedDoc.Sections.Add(new ParsedSection
            {
                SectionIndex = sectionIndex++,
                PageNumber = slideIndex,
                SectionTitle = slideTitle,
                Text = slideText.ToString(),
            });

            slideIndex++;
        }

        return parsedDoc;
    }
}
