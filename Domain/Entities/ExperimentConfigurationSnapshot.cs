namespace Domain.Entities;

public class ExperimentConfigurationSnapshot : NaturalEntity
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ChunkingStrategy { get; set; } = string.Empty;
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public string EmbeddingModel { get; set; } = string.Empty;
    public int TopK { get; set; }
    public double SimilarityThreshold { get; set; }
    public int MaxContextChunks { get; set; }
    public string LlmModel { get; set; } = string.Empty;
    public float ChatTemperature { get; set; }
    public int MaxHistoryMessages { get; set; }
    public string ChatPrompt { get; set; } = string.Empty;
    public string ContextPrompt { get; set; } = string.Empty;
    public string NoContextRetrievedPrompt { get; set; } = string.Empty;
    public string JudgeModel { get; set; } = string.Empty;
    public string EvaluatorPromptVersion { get; set; } = string.Empty;

    public virtual Experiment Experiment { get; set; } = null!;
}
