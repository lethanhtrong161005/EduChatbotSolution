namespace Domain.Exceptions;

/// <summary>
/// Thrown when supplied the request payload is invalid.
/// If the violations are entity data, consider the more specific <see cref="EntityValidationException"/>.
/// </summary>
public sealed class BadRequestException : Exception
{
    public string? Field { get; }

    public BadRequestException()
    {
    }

    public BadRequestException(string? message)
        : base(message)
    {
    }

    public BadRequestException(string? message, string? field)
        : base(message)
    {
        Field = field;
    }
}
