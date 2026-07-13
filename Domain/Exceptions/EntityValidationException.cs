namespace Domain.Exceptions;

/// <summary>
/// Thrown when supplied entity data is intrinsically invalid, independently
/// of the current state of the target or related persisted resources.
/// </summary>
public sealed class EntityValidationException : Exception
{
    public string? Property { get; }

    public EntityValidationException()
    {
    }

    public EntityValidationException(string? message)
        : base(message)
    {
    }

    public EntityValidationException(string? message, string? property)
        : base(message)
    {
        Property = property;
    }
}
