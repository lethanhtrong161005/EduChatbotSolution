using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public virtual ICollection<ApplicationUserRole> UserRoles { get; } = [];

    public virtual ICollection<ApplicationUser> Users { get; } = [];
}
