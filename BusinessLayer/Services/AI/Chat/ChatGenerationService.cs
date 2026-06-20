using Domain.Contracts;
using Domain.Contracts.DTOs;

namespace Business.Services.AI.Chat;

public class ChatGenerationService : IChatGenerationService
{
    public async Task<ChatGenerationResult> GenerateAsync(
        ChatGenerationRequest request,
        Func<string, Task> onToken,
        CancellationToken cxlTkn = default)
    {
        var answer =
        $"""
        This is a dummy response.

        You asked:

        "{request.UserMessage}"

        The streaming pipeline is working correctly.
        """;

        var tokens = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            cxlTkn.ThrowIfCancellationRequested();

            await onToken(token + " ");
            await Task.Delay(50, cxlTkn);
        }

        return new ChatGenerationResult
        {
            Answer = answer,
            ChunkRetrievals = [],
            ChunkRetrievalsInContext = [],
            ChunkUsages = [],
            Metrics = new ChatGenerationMetrics
            {
                PromptTokens = 0,
                CompletionTokens = tokens.Length,
                RetrievalTimeMs = 0,
                TimeToFirstTokenMs = 0,
                TotalResponseTimeMs = 0,
                TokensPerSecond = 0
            }
        };
    }
}
