namespace Domain.Contracts;

public interface IRagasStyleEvaluator
{
    Task<RagasStyleEvaluationResult> EvaluateAsync(RagasStyleEvaluationRequest request, CancellationToken cancellationToken = default);
}

public record RagasStyleEvaluationRequest
{
    public required string Question { get; init; }
    public required string GroundTruth { get; init; }
    public required string GeneratedAnswer { get; init; }
    public required IReadOnlyList<string> RetrievedContexts { get; init; }
    public required string JudgeModel { get; init; }
}

public record RagasStyleEvaluationResult
{
    public required double Faithfulness { get; init; }
    public required double AnswerRelevancy { get; init; }
    public required double ContextPrecision { get; init; }
    public required double ContextRecall { get; init; }
    public string? Explanation { get; init; }
}
