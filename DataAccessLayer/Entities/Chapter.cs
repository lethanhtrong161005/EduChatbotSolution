namespace DataAccessLayer.Entities;

public class Chapter : UuidEntity
{
    public Guid SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual Subject Subject { get; set; } = null!;
}
