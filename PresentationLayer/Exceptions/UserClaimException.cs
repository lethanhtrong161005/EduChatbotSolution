namespace PresentationLayer.Exceptions;

public class UserClaimException : Exception
{
    public UserClaimException()
    {
    }

    public UserClaimException(string message) : base(message)
    {
    }
}
