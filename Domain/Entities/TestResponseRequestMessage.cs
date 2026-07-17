namespace Domain.Entities;

public class TestResponseRequestMessage : NaturalEntity
{
    public Guid TestResponseId { get; set; }
    public int MessageOrder { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public virtual TestResponse TestResponse { get; set; } = null!;
}
