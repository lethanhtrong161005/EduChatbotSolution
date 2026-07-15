using Business.Services.AI.Chat;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Moq;
using System.Linq.Expressions;

namespace UnitTests;

public class ChatGenerationMetricsTests
{
    [Test]
    public async Task GenerateTitle_UsesProviderUsageWhenAvailable()
    {
        var client = new Mock<IChatClient>();
        client.Setup(item => item.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "A title"))
            {
                Usage = new UsageDetails { InputTokenCount = 11, OutputTokenCount = 4 },
            });

        var service = CreateService(client.Object);
        var result = await service.GenerateTitleAsync(TitleRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Metrics.PromptTokens, Is.EqualTo(11));
            Assert.That(result.Metrics.CompletionTokens, Is.EqualTo(4));
            Assert.That(result.Metrics.ResponseTimeMs, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public async Task GenerateTitle_StoresNullWhenProviderUsageIsUnavailable()
    {
        var client = new Mock<IChatClient>();
        client.Setup(item => item.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "A title")));

        var service = CreateService(client.Object);
        var result = await service.GenerateTitleAsync(TitleRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Metrics.PromptTokens, Is.Null);
            Assert.That(result.Metrics.CompletionTokens, Is.Null);
        }
    }

    [Test]
    public async Task GenerateChat_UsesStreamingUsageAndMeasuredTimings()
    {
        var client = new Mock<IChatClient>();
        client.Setup(item => item.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamWithUsage());

        var service = CreateService(client.Object);
        var result = await service.GenerateAnswerAsync(ChatRequest(), _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Metrics.PromptTokens, Is.EqualTo(20));
            Assert.That(result.Metrics.CompletionTokens, Is.EqualTo(5));
            Assert.That(result.Metrics.RetrievalTimeMs, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Metrics.TimeToFirstTokenMs, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Metrics.TotalResponseTimeMs, Is.GreaterThanOrEqualTo(result.Metrics.RetrievalTimeMs));
            Assert.That(result.Metrics.TokensPerSecond, Is.Not.Null);
        }
    }

    [Test]
    public async Task GenerateChat_LeavesThroughputNullWhenProviderReportsZeroCompletionTokens()
    {
        var client = new Mock<IChatClient>();
        client.Setup(item => item.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamWithUsage(outputTokens: 0));

        var service = CreateService(client.Object);
        var result = await service.GenerateAnswerAsync(ChatRequest(), _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Metrics.CompletionTokens, Is.Zero);
            Assert.That(result.Metrics.TokensPerSecond, Is.Null);
        }
    }

    private static ChatGenerationService CreateService(IChatClient client)
    {
        var embedder = new Mock<IEmbeddingService>();
        embedder.Setup(item => item.EmbedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbedResult { Model = "embed", Vectors = [new ReadOnlyMemory<float>([0.1f])] });

        var vectorSearch = new Mock<IVectorSearchService>();
        vectorSearch.Setup(item => item.SimilaritySearchCosineDistance(
                It.IsAny<ReadOnlyMemory<float>>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var factory = new Mock<IChatClientFactory>();
        factory.Setup(item => item.GetChatClient(It.IsAny<string>())).Returns(client);

        var dbContext = new Mock<DbContext>();
        var subjectRepository = new Mock<GenericRepository<Domain.Entities.Subject>>(dbContext.Object);
        subjectRepository.Setup(item => item.GetAsync(
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<Domain.Entities.Subject, bool>>>(),
                It.IsAny<Func<IQueryable<Domain.Entities.Subject>, IOrderedQueryable<Domain.Entities.Subject>>>(),
                It.IsAny<(int, int)>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Domain.Entities.Subject { Id = 1, Code = "DB201", Name = "Database Systems", IndexAvailability = Domain.Entities.SubjectIndexAvailability.Ready }]);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.Subjects).Returns(subjectRepository.Object);

        return new ChatGenerationService(embedder.Object, vectorSearch.Object, factory.Object, unitOfWork.Object);
    }

    private static TitleGenerationRequest TitleRequest() => new()
    {
        UserMessage = "Question",
        AssistantMessage = "Answer",
        Attachments = [],
        Settings = new TitleGenerationSettings
        {
            LlmModel = "model",
            Temperature = 0,
            SystemPrompt = "title",
        },
    };

    private static ChatGenerationRequest ChatRequest() => new()
    {
        UserMessage = "Question",
        AllowedSubjects = [1],
        ChatHistory = [new ChatHistoryMessage { ChatRole = Domain.Entities.ChatRole.User, Content = "Question" }],
        Settings = new ChatGenerationSettings
        {
            EmbeddingModel = "embed",
            TopK = 5,
            SimilarityThreshold = 0.5,
            LlmModel = "model",
            Temperature = 0,
            SystemPrompt = "system",
            ContextPrompt = "context",
            NoContextRetrievedPrompt = "none",
            CitationExtractionTemperature = 0,
            CitationExtractionPrompt = "cite",
            MaxContextChunks = 3,
            MaxHistoryMessages = 12,
        },
    };

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamWithUsage(long outputTokens = 5)
    {
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, "answer");
        yield return new ChatResponseUpdate
        {
            Contents = [new UsageContent(new UsageDetails { InputTokenCount = 20, OutputTokenCount = outputTokens })],
        };
    }
}
