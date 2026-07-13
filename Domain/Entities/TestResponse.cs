using Domain.Contracts.DTOs;

namespace Domain.Entities;

public class TestResponse : NaturalEntity
{
    public Guid ExperimentId { get; set; }
    public int TestQuestionId { get; set; }
    public ExperimentQuestionStatus Status { get; set; } = ExperimentQuestionStatus.Pending;
    public string? GeneratedAnswer { get; set; }
    public double? Faithfulness { get; set; }
    public double? AnswerRelevancy { get; set; }
    public double? ContextPrecision { get; set; }
    public double? ContextRecall { get; set; }
    public string? Explanation { get; set; }
    public string? FailureReason { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public long? RetrievalTimeMs { get; set; }
    public long? TimeToFirstTokenMs { get; set; }
    public long? TotalResponseTimeMs { get; set; }

    public virtual Experiment Experiment { get; set; } = null!;
    public virtual TestQuestion TestQuestion { get; set; } = null!;
    public virtual ICollection<TestResponseContext> RetrievedContexts { get; } = [];
}
