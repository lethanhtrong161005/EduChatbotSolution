namespace DataAccessLayer.Entities;

public class Subject : UuidEntity
{
    public string SubjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Chapter> Chapters { get; set; } = [];
}
