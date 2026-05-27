namespace DataAccessLayer.Entities;

public class Chunk : UuidEntity
{
    public Guid DocumentId { get; set; }
    public int ChunkIdex { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ChunkStrategy { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string VectorId { get; set; } = string.Empty;

    public virtual Document Document { get; set; } = null!;
}
