using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public abstract class NaturalEntity : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
}
