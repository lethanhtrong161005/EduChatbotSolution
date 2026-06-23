namespace Domain.Entities;

public class CitationOccurrence : NaturalEntity
{
    public Guid CitationId { get; set; }

    public int OccurrenceIndex { get; set; }

    public string SupportingQuote { get; set; } = string.Empty;

    public virtual Citation Citation { get; set; } = null!;
}
