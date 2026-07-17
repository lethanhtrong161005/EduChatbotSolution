using DataAccess.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

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
                    .SequenceEqual([nameof(TestResponseContext.TestResponseId), nameof(TestResponseContext.RetrievalRank)])), Is.True);
            Assert.That(chatMessageContext!.GetIndexes().Any(index =>
                index.IsUnique && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(ChatMessageContext.ChatMessageId), nameof(ChatMessageContext.RetrievalRank)])), Is.True);
        }
    }

    [Test]
    public void PersistedOrdinalModels_RequirePositiveUniqueParentSequences()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        using (Assert.EnterMultipleScope())
        {
            AssertOrdinal<ParsedSection>(nameof(ParsedSection.DocumentId), nameof(ParsedSection.SectionIndex), "ck_parsed_sections_section_index");
            AssertOrdinal<Chunk>(nameof(Chunk.DocumentId), nameof(Chunk.ChunkIndex), "ck_chunks_chunk_index");
            AssertOrdinal<ChatMessageContext>(nameof(ChatMessageContext.ChatMessageId), nameof(ChatMessageContext.RetrievalRank), "ck_chat_message_contexts_retrieval_rank");
            AssertOrdinal<TestResponseContext>(nameof(TestResponseContext.TestResponseId), nameof(TestResponseContext.RetrievalRank), "ck_test_response_contexts_retrieval_rank");
            AssertOrdinal<CitationOccurrence>(nameof(CitationOccurrence.CitationId), nameof(CitationOccurrence.OccurrenceIndex), "ck_citation_occurrences_occurrence_index");
        }

        void AssertOrdinal<TEntity>(string parentProperty, string indexProperty, string constraintName)
        {
            var entity = model.FindEntityType(typeof(TEntity));
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([parentProperty, indexProperty])), Is.True, $"{typeof(TEntity).Name} lacks a unique parent/index constraint.");
            Assert.That(entity.GetCheckConstraints().Any(constraint => constraint.Name == constraintName && constraint.Sql.EndsWith(">= 1", StringComparison.Ordinal)), Is.True, $"{typeof(TEntity).Name} lacks a positive-index check constraint.");
        }
    }

    [Test]
    public void ReconstructionModels_PersistAuthoritativeRequestsSubjectsEvidenceAndQuestionInputs()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var chatContext = model.FindEntityType(typeof(ChatMessageContext));
        var experimentContext = model.FindEntityType(typeof(TestResponseContext));
        var chatRequest = model.FindEntityType(typeof(ChatMessageRequestMessage));
        var experimentRequest = model.FindEntityType(typeof(TestResponseRequestMessage));
        var subjectScope = model.FindEntityType(typeof(ChatMessageSubjectSnapshot));
        var testResponse = model.FindEntityType(typeof(TestResponse));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chatContext!.FindProperty(nameof(ChatMessageContext.RetrievalRank)), Is.Not.Null);
            Assert.That(chatContext.FindProperty(nameof(ChatMessageContext.PromptOrder)), Is.Not.Null);
            Assert.That(chatContext.FindProperty(nameof(ChatMessageContext.SourceChunkId)), Is.Not.Null);
            Assert.That(chatContext.FindProperty(nameof(ChatMessageContext.ChunkText)), Is.Not.Null);
            Assert.That(experimentContext!.FindProperty(nameof(TestResponseContext.RetrievalRank)), Is.Not.Null);
            Assert.That(experimentContext.FindProperty(nameof(TestResponseContext.PromptOrder)), Is.Not.Null);
            Assert.That(chatRequest!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(ChatMessageRequestMessage.ChatMessageId), nameof(ChatMessageRequestMessage.MessageOrder)])), Is.True);
            Assert.That(experimentRequest!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(TestResponseRequestMessage.TestResponseId), nameof(TestResponseRequestMessage.MessageOrder)])), Is.True);
            Assert.That(subjectScope!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(ChatMessageSubjectSnapshot.ChatMessageId), nameof(ChatMessageSubjectSnapshot.SubjectOrder)])), Is.True);
            Assert.That(testResponse!.FindProperty(nameof(TestResponse.Question)), Is.Not.Null);
            Assert.That(testResponse.FindProperty(nameof(TestResponse.GroundTruth)), Is.Not.Null);
            Assert.That(testResponse.FindProperty(nameof(TestResponse.DatasetKey)), Is.Not.Null);
            Assert.That(testResponse.FindProperty(nameof(TestResponse.ReconstructionCompleteness)), Is.Not.Null);
            Assert.That(testResponse.FindProperty(nameof(TestResponse.TestQuestionId))!.IsNullable, Is.True);
        }
    }

    [Test]
    public void EvaluationModels_AreAppendOnlyAndUseOneBasedAttempts()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var response = model.FindEntityType(typeof(TestResponse));
        var attempt = model.FindEntityType(typeof(TestResponseEvaluationAttempt));
        var metric = model.FindEntityType(typeof(TestResponseEvaluationMetric));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response!.FindProperty(nameof(TestResponse.CurrentEvaluationAttemptId))!.IsNullable, Is.True);
            Assert.That(attempt!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(TestResponseEvaluationAttempt.TestResponseId), nameof(TestResponseEvaluationAttempt.AttemptNumber)])), Is.True);
            Assert.That(attempt.GetCheckConstraints().Any(constraint => constraint.Name == "ck_test_response_evaluation_attempts_attempt_number" && constraint.Sql == "attempt_number >= 1"), Is.True);
            Assert.That(metric!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(TestResponseEvaluationMetric.EvaluationAttemptId), nameof(TestResponseEvaluationMetric.MetricName)])), Is.True);
            Assert.That(response.GetForeignKeys().Single(foreignKey => foreignKey.Properties.Any(property => property.Name == nameof(TestResponse.CurrentEvaluationAttemptId))).DeleteBehavior, Is.EqualTo(DeleteBehavior.SetNull));
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
