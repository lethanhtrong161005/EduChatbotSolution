using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public class CategoryEntity : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
}
