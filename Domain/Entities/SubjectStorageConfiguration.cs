using Domain.Utils;

namespace Domain.Entities;

public class SubjectStorageConfiguration : CategoryLikeEntity
{
    public FileStorageMethod? StorageMethod { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
