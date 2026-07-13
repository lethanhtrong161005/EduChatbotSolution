using Domain.Contracts.DTOs;

namespace Domain.Entities;

public class Experiment : NaturalEntity
{
    public string ExperimentName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public ExperimentStatus Status { get; set; } = ExperimentStatus.Queued;
    public string QuestionSetKey { get; set; } = string.Empty;
    public int IndexedDocumentCount { get; set; }
    public int AffectedDocumentCount { get; set; }
    public int CompletedQuestionCount { get; set; }
    public int TotalQuestionCount { get; set; }
    public double? Faithfulness { get; set; }
    public double? AnswerRelevancy { get; set; }
    public double? ContextPrecision { get; set; }
    public double? ContextRecall { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }

    public virtual Subject Subject { get; set; } = null!;
    public virtual ExperimentConfigurationSnapshot ConfigurationSnapshot { get; set; } = null!;
    public virtual ICollection<TestResponse> TestResponses { get; } = [];
}
