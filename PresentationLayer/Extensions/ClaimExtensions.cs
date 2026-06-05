using Domain.Exceptions;
using System.Security.Claims;

namespace Presentation.Extensions;

public static class ClaimExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");
        return userId;
    }
}
