namespace Domain.Entities;

public class TestResponseEvaluationAttempt : NaturalEntity
{
    public Guid TestResponseId { get; set; }
    public int AttemptNumber { get; set; }
    public EvaluationAttemptStatus Status { get; set; } = EvaluationAttemptStatus.Pending;
    public string EvaluatorFamily { get; set; } = string.Empty;
    public string ContractVersion { get; set; } = string.Empty;
    public string ServiceVersion { get; set; } = string.Empty;
    public string RagasVersion { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string LlmProvider { get; set; } = string.Empty;
    public string LlmModel { get; set; } = string.Empty;
    public string EmbeddingProvider { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string MetricSetKey { get; set; } = string.Empty;
    public string EvaluatorProfileKey { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Summary { get; set; }
    public string? FailureReason { get; set; }

    public virtual TestResponse TestResponse { get; set; } = null!;
    public virtual ICollection<TestResponseEvaluationMetric> Metrics { get; } = [];
}

public enum EvaluationAttemptStatus { Pending = 0, Running = 1, Completed = 2, PartiallyCompleted = 3, Failed = 4 }
