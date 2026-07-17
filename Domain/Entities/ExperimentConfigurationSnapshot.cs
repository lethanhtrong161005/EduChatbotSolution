namespace Domain.Entities;

public class ExperimentConfigurationSnapshot : NaturalEntity
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ChunkingStrategy { get; set; } = string.Empty;
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public string EmbeddingProvider { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int TopK { get; set; }
    public double SimilarityThreshold { get; set; }
    public int MaxContextChunks { get; set; }
    public int MaxHistoryMessages { get; set; }
    public string LlmProvider { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public float ChatTemperature { get; set; }
    public string ChatPrompt { get; set; } = string.Empty;
    public string ContextPrompt { get; set; } = string.Empty;
    public string NoContextRetrievedPrompt { get; set; } = string.Empty;
    public float CitationExtractionTemperature { get; set; }
    public string CitationExtractionPrompt { get; set; } = string.Empty;
    public string EvaluatorLlmProvider { get; set; } = string.Empty;
    public string EvaluatorLlmModel { get; set; } = string.Empty;
    public string EvaluatorEmbeddingProvider { get; set; } = string.Empty;
    public string EvaluatorEmbeddingModel { get; set; } = string.Empty;
    public string EvaluatorMetricSetKey { get; set; } = string.Empty;
    public string EvaluatorPromptVersion { get; set; } = string.Empty;

    public virtual Experiment Experiment { get; set; } = null!;
}
