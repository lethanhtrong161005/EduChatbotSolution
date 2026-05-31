using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Gets or sets the user's full display name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the user account is active.</summary>
    public bool IsActive { get; set; } = true;

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the conversations belonging to this user.</summary>
    public virtual ICollection<Conversation> Conversations { get; set; } = [];
}
