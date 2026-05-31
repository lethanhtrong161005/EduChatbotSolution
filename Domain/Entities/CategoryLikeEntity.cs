using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public abstract class CategoryLikeEntity : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
}
