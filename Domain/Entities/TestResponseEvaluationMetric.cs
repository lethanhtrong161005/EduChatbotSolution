namespace Domain.Entities;

public class TestResponseEvaluationMetric : NaturalEntity
{
    public Guid EvaluationAttemptId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public EvaluationMetricStatus Status { get; set; } = EvaluationMetricStatus.Pending;
    public double? Score { get; set; }
    public string? Reason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public long? DurationMs { get; set; }

    public virtual TestResponseEvaluationAttempt EvaluationAttempt { get; set; } = null!;
}

public enum EvaluationMetricStatus { Pending = 0, Running = 1, Completed = 2, Failed = 3 }
