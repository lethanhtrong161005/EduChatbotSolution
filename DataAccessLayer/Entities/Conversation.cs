namespace DataAccessLayer.Entities;

public class Conversation : NaturalEntity
{
    public Guid UserId { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
