namespace Domain.Common;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class RetryAttribute : Attribute
{
    public int Retries { get; init; } = 5;
}
