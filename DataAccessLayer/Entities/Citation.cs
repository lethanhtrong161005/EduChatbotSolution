namespace DataAccessLayer.Entities;

public class Citation : NaturalEntity
{
    public Guid MessageId { get; set; }
    public Guid ChunkId { get; set; }
    public string QuotedText { get; set; } = string.Empty;
}
