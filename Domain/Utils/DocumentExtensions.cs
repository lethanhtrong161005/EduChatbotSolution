using Domain.Entities;

namespace Domain.Utils;

public static class DocumentExtensions
{
    public static string? BuildLocation(this Chunk chunk) => BuildLocation(chunk.PageNumber, chunk.SectionTitle);

    public static string? BuildLocation(this ParsedSection section) => BuildLocation(section.PageNumber, section.SectionTitle);

    private static string? BuildLocation(int? pageNumber, string? sectionTitle)
    {
        var locations = new List<string>();
        if (pageNumber != null)
        {
            locations.Add($"Page: {pageNumber}");
        }
        if (!string.IsNullOrWhiteSpace(sectionTitle))
        {
            locations.Add($"Section: {sectionTitle}");
        }
        return locations.Count > 0
            ? string.Join(" • ", locations)
            : null;
    }
}
