using Domain.Entities;

namespace Domain.Utils;

public static class DocumentExtensions
{
    public static string? BuildLocation(this Chunk chunk) => BuildLocation(chunk.StartPageNumber, chunk.EndPageNumber, chunk.StartSectionTitle, chunk.EndSectionTitle);

    public static string? BuildLocation(this ParsedSection section) => BuildLocation(section.PageNumber, section.SectionTitle);

    private static string? BuildLocation(int? startPageNumber, int? endPageNumber, string? startSectionTitle, string? endSectionTitle)
    {
        var locations = new List<string>();

        if (startPageNumber != null)
        {
            if (endPageNumber != null && endPageNumber > startPageNumber)
                locations.Add($"Page: {startPageNumber}–{endPageNumber}");
            else
                locations.Add($"Page: {startPageNumber}");
        }

        var startTitle = string.IsNullOrWhiteSpace(startSectionTitle) ? null : startSectionTitle;
        var endTitle = string.IsNullOrWhiteSpace(endSectionTitle) ? null : endSectionTitle;
        var title = startTitle ?? endTitle;

        if (startTitle != null && endTitle != null && endTitle != startTitle)
            locations.Add($"Section: {startTitle} → {endTitle}");
        else if (title != null)
            locations.Add($"Section: {title}");

        return locations.Count > 0 ? string.Join(" • ", locations) : null;
    }

    private static string? BuildLocation(int? pageNumber, string? sectionTitle) => BuildLocation(pageNumber, null, sectionTitle, null);
}
