namespace Domain.Exceptions;

/// <summary>
/// Thrown when a required user claim is missing or invalid.
/// </summary>
public class UserClaimException : Exception
{
    public UserClaimException()
    {
    }

    public UserClaimException(string message) : base(message)
    {
    }
}
