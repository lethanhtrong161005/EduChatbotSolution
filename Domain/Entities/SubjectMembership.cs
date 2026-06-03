namespace Domain.Entities;

public class SubjectMembership : NaturalEntity
{
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }

    public MembershipRole Role { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
}

public enum MembershipRole
{
    Student,
    Lecturer,
    Chief,
}
