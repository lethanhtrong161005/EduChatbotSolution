namespace Domain.Entities;

public class ParsedSection : NaturalEntity
{
    public Guid DocumentId { get; set; }

    public int? PageNumber { get; set; }

    public string? SectionTitle { get; set; }

    public string Text { get; set; } = "";

    public virtual Document Document { get; set; } = null!;
}
