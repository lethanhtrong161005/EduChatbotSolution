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

    public async Task<TitleGenerationResult> GenerateTitleAsync(
        TitleGenerationRequest req,
        CancellationToken cxlTkn = default)
    {
        var chatMessages = GetTitleChatMessages(req);

        var chatOpts = new ChatOptions
        {
            Temperature = req.Settings.Temperature,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.Low,
                Output = ReasoningOutput.None,
            },
        };

        var chatClient = _chatClientFactory.GetChatClient(req.Settings.LlmModel);

        var response = await chatClient.GetResponseAsync(chatMessages, chatOpts, cxlTkn);

        var result = new TitleGenerationResult
        {
            Title = response.Text,
            Metrics = new TitleGenerationMetrics
            {
                PromptTokens = 0,
                CompletionTokens = 0,
                ResponseTimeMs = 0,
            },
        };

        return result;
    }

    private static List<ChatMessage> GetTitleChatMessages(TitleGenerationRequest req)
    {
        var sb = new StringBuilder();

        sb.AppendLine("User:");
        sb.AppendLine(req.UserMessage);
        sb.AppendLine();

        sb.AppendLine("Assistant:");
        sb.AppendLine(req.AssistantMessage);
        sb.AppendLine();

        var input = sb.ToString();

        return
        [
            new(ChatRole.System, req.Settings.SystemPrompt),
            new(ChatRole.User, input),
        ];
    }

    public async Task<ChatGenerationResult> GenerateChatAsync(
        ChatGenerationRequest req,
        Func<string, Task> onToken,
        CancellationToken cxlTkn = default)
    {
        var chunkRetrievals = await RetrieveChunksAsync(req, cxlTkn);
        var chunkRetrievalsInContext = chunkRetrievals.Take(req.Settings.MaxContextChunks).ToList();

        var chatMessages = GetChatMessages(
            req,
            chunkRetrievalsInContext,
            [.. req.ChatHistory]);

        var chatOpts = new ChatOptions
        {
            Temperature = req.Settings.Temperature,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.Low,
                Output = ReasoningOutput.None,
            },
        };

        var chatClient = _chatClientFactory.GetChatClient(req.Settings.LlmModel);

        var rawAnswerSb = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, chatOpts, cxlTkn))
        {
            rawAnswerSb.Append(update.Text);
            await onToken(update.Text);
        }

        var rawAnswer = rawAnswerSb.ToString();

        var (processedAnswer, chunkUsages) = ProcessAnswer(rawAnswer, chunkRetrievalsInContext);

        return new ChatGenerationResult
        {
            Answer = processedAnswer,
            RawAnswer = rawAnswer,
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
        ChatGenerationRequest req,
        CancellationToken cxlTkn)
    {
        var embedResult = await _embedder.EmbedAsync(
            [req.UserMessage],
            req.Settings.EmbeddingModel,
            cxlTkn);

        var embedding = embedResult.Vectors[0];

        return await _vectorSearcher.SimilaritySearchCosineDistance(
            embedding,
            req.Settings.TopK,
            req.Settings.SimilarityThreshold,
            req.AllowedSubjects,
            cxlTkn);
    }

    private static List<ChatMessage> GetChatMessages(
        ChatGenerationRequest req,
        List<ChunkRetrieval> chunkRetrievalsInContext,
        List<ChatHistoryMessage> chatHistory)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, req.Settings.SystemPrompt),
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
                return req.Settings.NoContextRetrievedPrompt;
            }

            var sb = new StringBuilder();

            sb.AppendLine(req.Settings.ContextPrompt);
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

    private static (string ProcessAnswer, List<ChunkUsage> ChunkUsages) ProcessAnswer(
        string answer,
        List<ChunkRetrieval> chunkRetrievalsInContext)
    {
        var chunkUsages = new List<ChunkUsage>();

        var indexMap = new Dictionary<int, int>();

        var processedAnswer = CitationRegex.Replace(answer, match =>
            {
                if (!int.TryParse(match.Groups[1].Value, out var chunkRetrievalIndex))
                    return string.Empty;

                if (chunkRetrievalIndex < 1
                    || chunkRetrievalIndex > chunkRetrievalsInContext.Count)
                {
                    return string.Empty;
                }

                var nextCitationIndex = indexMap.Count + 1;

                if (indexMap.TryAdd(chunkRetrievalIndex, nextCitationIndex))
                {
                    var usedChunkRetrieval = chunkRetrievalsInContext[chunkRetrievalIndex - 1];

                    // TODO: Get citation occurrences w/ actual quotes.
                    chunkUsages.Add(new ChunkUsage
                    {
                        ChunkId = usedChunkRetrieval.ChunkId,
                        CitationIndex = nextCitationIndex,
                        SimilarityScore = usedChunkRetrieval.SimilarityScore,
                    });
                }

                var citationIndex = indexMap[chunkRetrievalIndex];

                return RenderCitationMarkup(citationIndex);
            });

        var cleanedAnswer = processedAnswer.Trim();

        return (cleanedAnswer, chunkUsages);

        static string RenderCitationMarkup(int citationIndex)
            => $@"[[{citationIndex}]]";
    }

    private async Task<List<CitationExtractionItem>?> ExtractCitationsAsync(
        ChatGenerationRequest req,
        string rawAnswer,
        List<ChunkRetrieval> chunkRetrievalsInContext,
        CancellationToken cxlTkn = default)
    {
        var input = BuildCitationExtractionInput();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, req.Settings.CitationExtractionPrompt),
            new(ChatRole.User, input),
        };

        var chatOpts = new ChatOptions
        {
            Temperature = req.Settings.CitationExtractionTemperature,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.High,
                Output = ReasoningOutput.None,
            },
        };

        var chatClient = _chatClientFactory.GetChatClient(req.Settings.LlmModel);

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

    private static string BuildPrompt(
        ChatGenerationRequest req,
        IReadOnlyList<ChunkRetrieval> contextChunks,
        IReadOnlyList<ChatHistoryMessage> chatHistory)
    {
        return $"""
            SYSTEM:
            {req.Settings.SystemPrompt}

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
                {req.UserMessage}
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
