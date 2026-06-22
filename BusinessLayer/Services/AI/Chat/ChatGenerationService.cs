using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.RegularExpressions;

namespace Business.Services.AI.Chat;

public class ChatGenerationService(
    IEmbeddingService embedder,
    IVectorSearchService vectorSearcher,
    IChatClientFactory chatClientFactory)
    : IChatGenerationService
{
    private readonly IEmbeddingService _embedder = embedder;
    private readonly IVectorSearchService _vectorSearcher = vectorSearcher;
    private readonly IChatClientFactory _chatClientFactory = chatClientFactory;

    private static readonly Regex CitationRegex = ChatGenerationServiceRegexes.CitationRegex();
    private static readonly Regex ConsecutiveWhitespaceRegex = ChatGenerationServiceRegexes.ConsecutiveWhitespaceRegex();

    public async Task<ChatGenerationResult> GenerateAsync(
        ChatGenerationRequest request,
        Func<string, Task> onToken,
        CancellationToken cxlTkn = default)
    {
        var chunkRetrievals = await RetrieveChunksAsync(request, cxlTkn);
        var chunkRetrievalsInContext = chunkRetrievals.Take(request.Settings.MaxContextChunks).ToList();

        var chatMessages = GetChatMessages(request, chunkRetrievalsInContext, [.. request.ChatHistory]);

        var chatOpts = new ChatOptions
        {
            Temperature = request.Settings.Temperature,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.Medium,
                Output = ReasoningOutput.None,
            },
        };

        var chatClient = _chatClientFactory.GetChatClient(request.Settings.LlmModel);

        var rawAnswerSb = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, chatOpts, cxlTkn))
        {
            rawAnswerSb.Append(update.Text);
            await onToken(update.Text);
        }

        var rawAnswer = rawAnswerSb.ToString();

        var extractedCitations = await ExtractCitationsAsync(
            request,
            rawAnswer,
            chunkRetrievalsInContext,
            cxlTkn);

        var (processedAnswer, chunkUsages) = ProcessAnswer(
            rawAnswer,
            chunkRetrievalsInContext,
            extractedCitations ?? []);

        return new ChatGenerationResult
        {
            Answer = processedAnswer,
            ChunkRetrievals = chunkRetrievals,
            ChunkRetrievalsInContext = chunkRetrievalsInContext,
            ChunkUsages = chunkUsages,
            Metrics = new ChatGenerationMetrics
            {
                PromptTokens = 0,
                CompletionTokens = 0,
                RetrievalTimeMs = 0,
                TimeToFirstTokenMs = 0,
                TotalResponseTimeMs = 0,
                TokensPerSecond = 0,
            },
        };
    }

    private async Task<IReadOnlyList<ChunkRetrieval>> RetrieveChunksAsync(
        ChatGenerationRequest request,
        CancellationToken cxlTkn)
    {
        var embedResult = await _embedder.EmbedAsync(
            [request.UserMessage],
            request.Settings.EmbeddingModel,
            cxlTkn);

        var embedding = embedResult.Vectors[0];

        return await _vectorSearcher.SimilaritySearchCosineDistance(
            embedding,
            request.Settings.TopK,
            request.Settings.SimilarityThreshold,
            request.AllowedSubjects,
            cxlTkn);
    }

    private static List<ChatMessage> GetChatMessages(
        ChatGenerationRequest request,
        List<ChunkRetrieval> chunkRetrievalsInContext,
        List<ChatHistoryMessage> chatHistory)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, request.Settings.SystemPrompt),
            new(ChatRole.System, BuildContext())
        };

        foreach (var historyMessage in chatHistory)
        {
            messages.Add(new ChatMessage(
                historyMessage.ChatRole switch
                {
                    Domain.Entities.ChatRole.User => ChatRole.User,
                    Domain.Entities.ChatRole.Assistant => ChatRole.Assistant,
                    _ => ChatRole.User,
                },
                historyMessage.Content
                ));
        }

        return messages;

        string BuildContext()
        {
            if (chunkRetrievalsInContext.Count == 0)
            {
                return request.Settings.NoContextRetrievedPrompt;
            }

            var sb = new StringBuilder();

            sb.AppendLine(request.Settings.ContextPrompt);
            sb.AppendLine();

            for (int i = 0; i < chunkRetrievalsInContext.Count; i++)
            {
                sb.AppendLine($"[Chunk {i + 1}]");
                sb.AppendLine(chunkRetrievalsInContext[i].ChunkText);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    private async Task<List<CitationExtractionItem>?> ExtractCitationsAsync(
        ChatGenerationRequest request,
        string rawAnswer,
        List<ChunkRetrieval> chunkRetrievalsInContext,
        CancellationToken cxlTkn = default)
    {
        var input = BuildCitationExtractionInput();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, request.Settings.CitationExtractionPrompt),
            new(ChatRole.User, input),
        };

        var chatOpts = new ChatOptions
        {
            Temperature = request.Settings.CitationExtractionTemperature,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.High,
                Output = ReasoningOutput.None,
            },
        };

        var chatClient = _chatClientFactory.GetChatClient(request.Settings.LlmModel);

        var response = await chatClient.GetResponseAsync<List<CitationExtractionItem>>(messages, chatOpts, true, cxlTkn);

        response.TryGetResult(out var citationExtractions);

        return citationExtractions;

        string BuildCitationExtractionInput()
        {
            var sb = new StringBuilder();

            sb.AppendLine("ANSWER:");
            sb.AppendLine(rawAnswer);
            sb.AppendLine();

            sb.AppendLine("CHUNKS:");

            for (int i = 0; i < chunkRetrievalsInContext.Count; i++)
            {
                sb.AppendLine($"[Chunk {i + 1}]");
                sb.AppendLine(chunkRetrievalsInContext[i].ChunkText);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    private static (string ProcessAnswer, List<ChunkUsage> ChunkUsages) ProcessAnswer(
        string answer,
        List<ChunkRetrieval> chunkRetrievalsInContext,
        List<CitationExtractionItem> citationExtractions)
    {
        var chunkUsages = new List<ChunkUsage>();

        var indexMap = new Dictionary<int, int>();

        var extractionLookup = citationExtractions
            .Where(e => e.Valid)
            .ToDictionary(e => (e.OccurrenceId, e.ChunkIndex));

        var processedAnswer = CitationRegex.Replace(answer, match =>
            {
                if (!int.TryParse(match.Groups[1].Value, out var chunkRetrievalIndex))
                    return string.Empty;

                if (chunkRetrievalIndex < 1
                    || chunkRetrievalIndex > chunkRetrievalsInContext.Count)
                {
                    return string.Empty;
                }

                indexMap.TryAdd(chunkRetrievalIndex, indexMap.Count + 1);

                var occurrenceIndex = chunkUsages.Count + 1;
                var citationIndex = indexMap[chunkRetrievalIndex];

                var usedChunkRetrieval = chunkRetrievalsInContext[chunkRetrievalIndex - 1];

                _ = extractionLookup.TryGetValue(
                    (occurrenceIndex, chunkRetrievalIndex),
                    out var citationExtractionItem);

                chunkUsages.Add(new ChunkUsage
                {
                    ChunkId = usedChunkRetrieval.ChunkId,
                    OccurrenceIndex = occurrenceIndex,
                    CitationIndex = citationIndex,
                    QuotedText = citationExtractionItem?.SupportingQuote,
                    SimilarityScore = usedChunkRetrieval.SimilarityScore,
                });

                return RenderCitationMarkup(citationIndex);
            });

        var cleanedAnswer = processedAnswer.Trim();

        return (cleanedAnswer, chunkUsages);

        static string RenderCitationMarkup(int citationIndex) => $"""
            <sup class="message-inline-citation" data-citation-index="{citationIndex}">[{citationIndex}]</sup>
            """;
    }

    private static string BuildPrompt(
        ChatGenerationRequest request,
        IReadOnlyList<ChunkRetrieval> contextChunks,
        IReadOnlyList<ChatHistoryMessage> chatHistory)
    {
        return $"""
            SYSTEM:
            {request.Settings.SystemPrompt}

            CONTEXT:
            {string.Join("\n\n", contextChunks.Select((c, index) => $"""
                [Chunk {index + 1}]
                {c.ChunkText}
                """)
            )}

            CHAT HISTORY:
            {string.Join("\n\n", chatHistory.Select(ch => $"""
                {ch.ChatRole}: {ch.Content}
                """)
            )}

            CURRENT QUESTION:
                {request.UserMessage}
            """;
    }
}

internal static partial class ChatGenerationServiceRegexes
{
    [GeneratedRegex(@"\{\{(\d+)\}\}", RegexOptions.Compiled)]
    public static partial Regex CitationRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    public static partial Regex ConsecutiveWhitespaceRegex();
}

internal class CitationExtractionItem
{
    public int OccurrenceId { get; set; }

    public int ChunkIndex { get; set; }

    public string? SupportingQuote { get; set; }

    public bool Valid { get; set; }
}
