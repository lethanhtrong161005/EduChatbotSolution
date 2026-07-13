namespace Domain.Entities;

public class TestResponseContext : NaturalEntity
{
    public Guid TestResponseId { get; set; }
    public int ContextIndex { get; set; }
    public string ContextText { get; set; } = string.Empty;

    public virtual TestResponse TestResponse { get; set; } = null!;
}
