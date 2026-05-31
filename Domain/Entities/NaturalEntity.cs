using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public abstract class NaturalEntity : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
}
