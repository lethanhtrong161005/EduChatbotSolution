namespace Domain.Exceptions;

/// <summary>
/// Thrown when a valid operation cannot be applied because it conflicts with
/// the current state of the target entity or a related persisted resource.
/// </summary>
public sealed class EntityConflictException : Exception
{
    public string? Property { get; }

    public EntityConflictException()
    {
    }

    public EntityConflictException(string? message)
        : base(message)
    {
    }

    public EntityConflictException(string? message, string? property)
        : base(message)
    {
        Property = property;
    }
}
