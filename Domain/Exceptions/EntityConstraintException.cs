namespace Domain.Exceptions;

/// <summary>
/// Thrown when an entity violates a database constraint (e.g., unique constraint).
/// </summary>
public class EntityConstraintException : Exception
{
    public string? Property { get; }

    public EntityConstraintException()
    {
    }

    public EntityConstraintException(string? message) : base(message)
    {
    }

    public EntityConstraintException(string? message, string? property) : base(message)
    {
        Property = property;
    }
}
