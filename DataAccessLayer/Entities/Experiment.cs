namespace DataAccessLayer.Entities;

public class Experiment : NaturalEntity
{
    public string Name { get; set; } = string.Empty;
    public string ChunkingStrategy { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string RetrievalMethod { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public double AverageRagasScore { get; set; }
    public string Notes { get; set; } = string.Empty;
}
