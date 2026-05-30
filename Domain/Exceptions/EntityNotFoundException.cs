namespace Domain.Exceptions;

/// <summary>
/// Thrown when an entity with the specified key is not found in the database.
/// </summary>
public class EntityNotFoundException : Exception
{
    private const string DefaultMessage = "No record matched the provided key {0}.";

    public EntityNotFoundException()
    {
    }

    public EntityNotFoundException(string? message) : base(message)
    {
    }

    public EntityNotFoundException(object id) : base(string.Format(DefaultMessage, id))
    {
    }
}
