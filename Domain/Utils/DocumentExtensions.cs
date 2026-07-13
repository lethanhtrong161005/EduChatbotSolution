using Domain.Entities;

namespace Domain.Utils;

public static class DocumentExtensions
{
    public static string? BuildLocation(this Chunk chunk) => BuildLocation(chunk.StartPageNumber, chunk.EndPageNumber, chunk.StartSectionTitle, chunk.EndSectionTitle);

    public static string? BuildLocation(this ParsedSection section) => BuildLocation(section.PageNumber, section.PageNumber, section.SectionTitle, section.SectionTitle);

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
        if (!string.IsNullOrWhiteSpace(startSectionTitle))
        {
            if (!string.IsNullOrWhiteSpace(endSectionTitle) && endSectionTitle != startSectionTitle)
                locations.Add($"Section: {startSectionTitle} → {endSectionTitle}");
            else
                locations.Add($"Section: {startSectionTitle}");
        }
        return locations.Count > 0
            ? string.Join(" • ", locations)
            : null;
    }
}
