namespace Domain.Entities;

public class DocumentComment : NaturalEntity
{
    public Guid DocumentId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;

    public virtual Document Document { get; set; } = null!;

    public virtual ApplicationUser Author { get; set; } = null!;
}
