using Domain.Constants;
using Domain.Contracts;
using Domain.Exceptions;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json.Serialization;

namespace Business.Services.AI.Experiments;

public sealed class RagasStyleEvaluator(IChatClientFactory chatClientFactory) : IRagasStyleEvaluator
{
    private readonly IChatClientFactory _chatClientFactory = chatClientFactory;

    private const string SystemPrompt = """
        You are a deterministic evaluator for a retrieval-augmented generation experiment.

        Evaluate the supplied question, reference answer, generated answer, and retrieved contexts using four independent scores from 0 to 1.

        faithfulness:
        How completely the factual claims in the generated answer are supported by the retrieved contexts. Do not reward unsupported claims merely because they match the reference answer.

        answer_relevancy:
        How directly and completely the generated answer addresses the question. Penalize irrelevant, evasive, or unnecessarily incomplete answers.

        context_precision:
        How relevant and useful the retrieved contexts are for answering the question. Penalize irrelevant or distracting contexts.

        context_recall:
        How completely the retrieved contexts contain the information needed to derive the reference answer.

        Use the full continuous range from 0 to 1. Return only the required structured result. Keep the explanation concise and mention the most important reason for the scores.
        """;

    public async Task<RagasStyleEvaluationResult> EvaluateAsync(RagasStyleEvaluationRequest request, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        //if (request.JudgeModel != ChatModelName.Gemini35Flash) throw new EntityValidationException($"The experiment judge must be '{ChatModelName.Gemini35Flash}'.", nameof(request.JudgeModel));

        var response = await _chatClientFactory.GetChatClient(request.JudgeModel).GetResponseAsync<RagasStyleJudgeOutput>(
            [new(ChatRole.System, SystemPrompt), new(ChatRole.User, BuildInput(request))],
            new ChatOptions { Temperature = 0, Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low, Output = ReasoningOutput.None } },
            true,
            cxlTkn);

        if (!response.TryGetResult(out var result) || result == null) throw new InvalidOperationException("The judge did not return a valid structured evaluation.");

        return new RagasStyleEvaluationResult
        {
            Faithfulness = ValidateScore(result.Faithfulness, nameof(result.Faithfulness)),
            AnswerRelevancy = ValidateScore(result.AnswerRelevancy, nameof(result.AnswerRelevancy)),
            ContextPrecision = ValidateScore(result.ContextPrecision, nameof(result.ContextPrecision)),
            ContextRecall = ValidateScore(result.ContextRecall, nameof(result.ContextRecall)),
            Explanation = string.IsNullOrWhiteSpace(result.Explanation) ? null : result.Explanation.Trim(),
        };
    }

    private static string BuildInput(RagasStyleEvaluationRequest request)
    {
        var contexts = new StringBuilder();
        if (request.RetrievedContexts.Count == 0) contexts.AppendLine("(No contexts were retrieved.)");
        else for (var i = 0; i < request.RetrievedContexts.Count; i++) contexts.AppendLine($"[Context {i + 1}]").AppendLine(request.RetrievedContexts[i]).AppendLine();

        return $"""
            QUESTION:
            {request.Question}

            REFERENCE ANSWER:
            {request.GroundTruth}

            GENERATED ANSWER:
            {request.GeneratedAnswer}

            RETRIEVED CONTEXTS:
            {contexts}
            """;
    }

    private static double ValidateScore(double value, string property)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new InvalidOperationException($"The judge returned an invalid {property} score: {value}.");
        return value;
    }

    private sealed class RagasStyleJudgeOutput
    {
        [JsonPropertyName("faithfulness")]
        public double Faithfulness { get; set; }

        [JsonPropertyName("answer_relevancy")]
        public double AnswerRelevancy { get; set; }

        [JsonPropertyName("context_precision")]
        public double ContextPrecision { get; set; }

        [JsonPropertyName("context_recall")]
        public double ContextRecall { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }
}
