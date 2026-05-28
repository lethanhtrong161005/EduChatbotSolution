namespace DataAccessLayer.Entities;

public class Chunk : NaturalEntity
{
    public Guid DocumentId { get; set; }
    public int ChunkIdex { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ChunkStrategy { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string VectorId { get; set; } = string.Empty;
    public int TokenCount { get; set; }

    public virtual Document Document { get; set; } = null!;
}
