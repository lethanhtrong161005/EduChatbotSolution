using Domain.Entities;
using System.Text.Json.Serialization;

namespace Domain.Contracts;

public interface IPythonRagasClient
{
    Task<PythonRagasHealth> GetHealthAsync(CancellationToken cancellationToken = default);
    Task<PythonRagasCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<PythonRagasEvaluationResponse> EvaluateAsync(PythonRagasEvaluationRequest request, CancellationToken cancellationToken = default);
}

public interface IExperimentEvaluationService
{
    Task<TestResponseEvaluationAttempt> AppendInitialEvaluationAsync(Guid testResponseId, CancellationToken cancellationToken = default);
}

public static class RagasMetricName
{
    public const string Faithfulness = "faithfulness";
    public const string AnswerRelevancy = "answer_relevancy";
    public const string ContextPrecision = "context_precision";
    public const string ContextRecall = "context_recall";
    public static readonly IReadOnlyList<string> All = [Faithfulness, AnswerRelevancy, ContextPrecision, ContextRecall];
}

public sealed record PythonRagasHealth
{
    [JsonPropertyName("status")] public required string Status { get; init; }
    [JsonPropertyName("contract_version")] public required string ContractVersion { get; init; }
    [JsonPropertyName("service_version")] public required string ServiceVersion { get; init; }
    [JsonPropertyName("ragas_version")] public required string RagasVersion { get; init; }
}

public sealed record PythonRagasModelOption
{
    [JsonPropertyName("provider")] public required string Provider { get; init; }
    [JsonPropertyName("model")] public required string Model { get; init; }
    [JsonPropertyName("label")] public required string Label { get; init; }
}

public sealed record PythonRagasCapabilities
{
    [JsonPropertyName("contract_version")] public required string ContractVersion { get; init; }
    [JsonPropertyName("service_version")] public required string ServiceVersion { get; init; }
    [JsonPropertyName("ragas_version")] public required string RagasVersion { get; init; }
    [JsonPropertyName("prompt_version")] public required string PromptVersion { get; init; }
    [JsonPropertyName("metrics")] public required IReadOnlyList<string> Metrics { get; init; }
    [JsonPropertyName("llm_options")] public required IReadOnlyList<PythonRagasModelOption> LlmOptions { get; init; }
    [JsonPropertyName("embedding_options")] public required IReadOnlyList<PythonRagasModelOption> EmbeddingOptions { get; init; }
}

public sealed record PythonRagasEvaluationRequest
{
    [JsonPropertyName("request_id")] public required Guid RequestId { get; init; }
    [JsonPropertyName("contract_version")] public required string ContractVersion { get; init; }
    [JsonPropertyName("language")] public required string Language { get; init; }
    [JsonPropertyName("question")] public required string Question { get; init; }
    [JsonPropertyName("reference")] public required string Reference { get; init; }
    [JsonPropertyName("response")] public required string Response { get; init; }
    [JsonPropertyName("prompt_contexts")] public required IReadOnlyList<string> PromptContexts { get; init; }
    [JsonPropertyName("retrieval_contexts")] public required IReadOnlyList<string> RetrievalContexts { get; init; }
    [JsonPropertyName("metrics")] public required IReadOnlyList<string> Metrics { get; init; }
    [JsonPropertyName("llm_provider")] public required string LlmProvider { get; init; }
    [JsonPropertyName("llm_model")] public required string LlmModel { get; init; }
    [JsonPropertyName("embedding_provider")] public required string EmbeddingProvider { get; init; }
    [JsonPropertyName("embedding_model")] public required string EmbeddingModel { get; init; }
    [JsonPropertyName("prompt_version")] public required string PromptVersion { get; init; }
}

public sealed record PythonRagasMetricResult
{
    [JsonPropertyName("metric_name")] public required string MetricName { get; init; }
    [JsonPropertyName("status")] public required string Status { get; init; }
    [JsonPropertyName("score")] public double? Score { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("error_code")] public string? ErrorCode { get; init; }
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; init; }
    [JsonPropertyName("duration_ms")] public long? DurationMs { get; init; }
}

public sealed record PythonRagasEvaluationResponse
{
    [JsonPropertyName("request_id")] public required Guid RequestId { get; init; }
    [JsonPropertyName("contract_version")] public required string ContractVersion { get; init; }
    [JsonPropertyName("service_version")] public required string ServiceVersion { get; init; }
    [JsonPropertyName("ragas_version")] public required string RagasVersion { get; init; }
    [JsonPropertyName("prompt_version")] public required string PromptVersion { get; init; }
    [JsonPropertyName("results")] public required IReadOnlyList<PythonRagasMetricResult> Results { get; init; }
}
