namespace Domain.Entities;

public class DocumentChapter : NaturalEntity
{
    public Guid DocumentId { get; set; }

    public int ChapterId { get; set; }

    public int SubjectId { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual Chapter Chapter { get; set; } = null!;
}
