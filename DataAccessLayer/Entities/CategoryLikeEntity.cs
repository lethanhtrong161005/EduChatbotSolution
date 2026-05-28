using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public abstract class CategoryLikeEntity : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
}
