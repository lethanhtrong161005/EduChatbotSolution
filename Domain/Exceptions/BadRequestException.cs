namespace Domain.Exceptions;

/// <summary>
/// Thrown when a client request is malformed or invalid.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException()
    {
    }

    public BadRequestException(string? message) : base(message)
    {
    }
}
