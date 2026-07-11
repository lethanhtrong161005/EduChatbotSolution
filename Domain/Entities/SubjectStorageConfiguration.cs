namespace Domain.Entities;

public class SubjectStorageConfiguration : CategoryLikeEntity
{
    public DocumentStorageMethod? StorageMethod { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
