using DataAccess.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

public class ChatAndExperimentDataModelTests
{
    [Test]
    public void GlobalConfiguration_UsesFrozenChunkDefaults()
    {
        var configuration = new GlobalAiConfiguration();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(configuration.ChunkSize, Is.EqualTo(1000));
            Assert.That(configuration.ChunkOverlap, Is.EqualTo(200));
        }
    }

    [Test]
    public void IndexCompatibilityFields_StartUnknownAndSubjectStartsReady()
    {
        var subject = new Subject();
        var document = new Document();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subject.IndexAvailability, Is.EqualTo(SubjectIndexAvailability.Ready));
            Assert.That(document.IndexedChunkingStrategy, Is.Null);
            Assert.That(document.IndexedChunkSize, Is.Null);
            Assert.That(document.IndexedChunkOverlap, Is.Null);
            Assert.That(document.IndexedEmbeddingModel, Is.Null);
        }
    }

    [Test]
    public void UsageMetrics_AllowUnavailableProviderValues()
    {
        var chatMetrics = new ChatMessageGenerationMetrics();
        var titleMetrics = new ChatSessionTitleGenerationMetrics();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chatMetrics.PromptTokens, Is.Null);
            Assert.That(chatMetrics.CompletionTokens, Is.Null);
            Assert.That(chatMetrics.TimeToFirstTokenMs, Is.Null);
            Assert.That(chatMetrics.TokensPerSecond, Is.Null);
            Assert.That(titleMetrics.PromptTokens, Is.Null);
            Assert.That(titleMetrics.CompletionTokens, Is.Null);
        }
    }

    [Test]
    public void ChatAndExperimentModels_HaveOrderedContextSnapshots()
    {
        using var context = CreateContext();
        var model = context.Model;

        var experiment = model.FindEntityType(typeof(Experiment));
        var snapshot = model.FindEntityType(typeof(ExperimentConfigurationSnapshot));
        var testResponseContext = model.FindEntityType(typeof(TestResponseContext));
        var chatMessageContext = model.FindEntityType(typeof(ChatMessageContext));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(experiment, Is.Not.Null);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(testResponseContext, Is.Not.Null);
            Assert.That(chatMessageContext, Is.Not.Null);
            Assert.That(testResponseContext!.GetIndexes().Any(index =>
                index.IsUnique && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(TestResponseContext.TestResponseId), nameof(TestResponseContext.ContextIndex)])), Is.True);
            Assert.That(chatMessageContext!.GetIndexes().Any(index =>
                index.IsUnique && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(ChatMessageContext.ChatMessageId), nameof(ChatMessageContext.ContextIndex)])), Is.True);
        }
    }

    private static EduChatAiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=phase2_model_only;Username=unused;Password=unused",
                npgsql => npgsql.UseVector())
            .Options;

        return new EduChatAiDbContext(options);
    }
}
