namespace DataAccessLayer.Entities;

public class User : UuidEntity
{
    public string Fullname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Student;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum Role
{
    Admin,
    Student,
    Lecturer,
}
