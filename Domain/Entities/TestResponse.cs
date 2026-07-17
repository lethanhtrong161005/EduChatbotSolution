using Domain.Contracts.DTOs;

namespace Domain.Entities;

public class TestResponse : NaturalEntity
{
    public Guid ExperimentId { get; set; }
    public int? TestQuestionId { get; set; }
    public int? SourceQuestionId { get; set; }
    public string? DatasetName { get; set; }
    public string? DatasetKey { get; set; }
    public string? DatasetVersion { get; set; }
    public string? QuestionExternalId { get; set; }
    public string? QuestionLanguage { get; set; }
    public string? QuestionDifficulty { get; set; }
    public string? Question { get; set; }
    public string? GroundTruth { get; set; }
    public ExperimentQuestionStatus Status { get; set; } = ExperimentQuestionStatus.Pending;
    public ReconstructionCompleteness ReconstructionCompleteness { get; set; } = ReconstructionCompleteness.LegacyIncomplete;
    public string? GeneratedAnswer { get; set; }
    public string? RawGeneratedAnswer { get; set; }
    public Guid? CurrentEvaluationAttemptId { get; set; }
    public string? FailureReason { get; set; }
    public long? PromptTokens { get; set; }
    public long? CompletionTokens { get; set; }
    public long? RetrievalTimeMs { get; set; }
    public long? TimeToFirstTokenMs { get; set; }
    public long? TotalResponseTimeMs { get; set; }

    public virtual Experiment Experiment { get; set; } = null!;
    public virtual TestQuestion? TestQuestion { get; set; }
    public virtual ICollection<TestResponseContext> RetrievedContexts { get; } = [];
    public virtual ICollection<TestResponseRequestMessage> RequestMessages { get; } = [];
    public virtual ICollection<TestResponseEvaluationAttempt> EvaluationAttempts { get; } = [];
    public virtual TestResponseEvaluationAttempt? CurrentEvaluationAttempt { get; set; }
}
