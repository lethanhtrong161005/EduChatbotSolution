namespace DataAccessLayer.Entities;

public class Citation : NaturalEntity
{
    public Guid MessageId { get; set; }
    public Guid ChunkId { get; set; }
    public string QuotedText { get; set; } = string.Empty;

    public virtual Message Message { get; set; } = null!;
    public virtual Chunk Chunk { get; set; } = null!;
}
