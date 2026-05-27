namespace DataAccessLayer.Exceptions;

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
