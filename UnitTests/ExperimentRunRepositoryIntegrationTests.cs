using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Constants;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

[NonParallelizable]
public class ExperimentRunRepositoryIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private string _connectionString = null!;

    [SetUp]
    public void SetUp()
    {
        _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString)) Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
    }

    [Test]
    public async Task CreateQueuedAsync_AllowsOnlyOneConcurrentActiveExperimentPerSubject()
    {
        var subjectId = await CreateSubjectAsync();

        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();

        var outcomes = await Task.WhenAll(
            AttemptCreateAsync(new ExperimentRunRepository(firstContext), NewExperiment(subjectId, "First")),
            AttemptCreateAsync(new ExperimentRunRepository(secondContext), NewExperiment(subjectId, "Second")));

        Assert.Multiple(() =>
        {
            Assert.That(outcomes.Count(e => e is null), Is.EqualTo(1));
            Assert.That(outcomes.Count(e => e is EntityConflictException), Is.EqualTo(1));
        });

        await using var assertionContext = CreateContext();
        Assert.That(await assertionContext.Experiments.CountAsync(e => e.SubjectId == subjectId), Is.EqualTo(1));
    }

    [Test]
    public async Task PrepareAsync_AtomicallyPersistsConfigurationAndAcquiresReindexState()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Failed);
        var experiment = NewExperiment(subjectId, "Prepare");

        await using (var setupContext = CreateContext())
            await new ExperimentRunRepository(setupContext).CreateQueuedAsync(experiment);

        await using (var actionContext = CreateContext())
        {
            var preparation = await new ExperimentRunRepository(actionContext).PrepareAsync(experiment.Id, Configuration(subjectId));

            Assert.Multiple(() =>
            {
                Assert.That(preparation, Is.Not.Null);
                Assert.That(preparation!.RequiresReindex, Is.True);
                Assert.That(preparation.AffectedDocumentCount, Is.Zero);
            });
        }

        await using var assertionContext = CreateContext();
        var subject = await assertionContext.Subjects.AsNoTracking().SingleAsync(e => e.Id == subjectId);
        var stored = await assertionContext.SubjectAiConfigurations.AsNoTracking().SingleAsync(e => e.Id == subjectId);
        var storedExperiment = await assertionContext.Experiments.AsNoTracking().SingleAsync(e => e.Id == experiment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(subject.IndexAvailability, Is.EqualTo(SubjectIndexAvailability.Reindexing));
            Assert.That(storedExperiment.Status, Is.EqualTo(ExperimentStatus.PreparingIndex));
            Assert.That(stored.ChunkingStrategy, Is.EqualTo(ChunkingStrategy.RecursiveSeparator));
            Assert.That(stored.ChunkSize, Is.EqualTo(1200));
            Assert.That(stored.EmbeddingModel, Is.EqualTo(EmbeddingModelName.BgeM3));
            Assert.That(stored.LlmModel, Is.EqualTo(ChatModelName.Qwen3));
        });
    }

    [Test]
    public async Task EvaluationAttempts_AppendOneBasedAndSelectingCurrentPreservesEarlierAttempts()
    {
        var responseId = await CreateAnsweredResponseAsync();
        Guid firstId;

        await using (var firstContext = CreateContext())
        {
            var repository = new ExperimentEvaluationRepository(firstContext);
            var first = await repository.AppendAttemptAsync(responseId, Attempt());
            first.Status = EvaluationAttemptStatus.Completed;
            first.Metrics.Add(new TestResponseEvaluationMetric { MetricName = "faithfulness", Status = EvaluationMetricStatus.Completed, Score = .75 });
            await firstContext.SaveChangesAsync();
            await repository.SelectCurrentAsync(responseId, first.Id);
            firstId = first.Id;
        }

        await using (var secondContext = CreateContext())
        {
            var repository = new ExperimentEvaluationRepository(secondContext);
            var second = await repository.AppendAttemptAsync(responseId, Attempt());
            second.Status = EvaluationAttemptStatus.PartiallyCompleted;
            second.Metrics.Add(new TestResponseEvaluationMetric { MetricName = "faithfulness", Status = EvaluationMetricStatus.Completed, Score = .9 });
            second.Metrics.Add(new TestResponseEvaluationMetric { MetricName = "context_recall", Status = EvaluationMetricStatus.Failed, ErrorCode = "evaluation_error" });
            await secondContext.SaveChangesAsync();
            await repository.SelectCurrentAsync(responseId, second.Id);
        }

        await using var assertionContext = CreateContext();
        var response = await assertionContext.TestResponses.AsNoTracking().Include(e => e.EvaluationAttempts).ThenInclude(e => e.Metrics).SingleAsync(e => e.Id == responseId);
        var attempts = response.EvaluationAttempts.OrderBy(e => e.AttemptNumber).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempts.Select(e => e.AttemptNumber), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(attempts[0].Id, Is.EqualTo(firstId));
            Assert.That(attempts[0].Metrics.Single().Score, Is.EqualTo(.75));
            Assert.That(attempts[1].Metrics.Count, Is.EqualTo(2));
            Assert.That(response.CurrentEvaluationAttemptId, Is.EqualTo(attempts[1].Id));
        }
    }

    [Test]
    public async Task SelectCurrentAsync_RunningAttemptIsRejected()
    {
        var responseId = await CreateAnsweredResponseAsync();
        await using var context = CreateContext();
        var repository = new ExperimentEvaluationRepository(context);
        var attempt = await repository.AppendAttemptAsync(responseId, Attempt());
        attempt.Status = EvaluationAttemptStatus.Running;
        await context.SaveChangesAsync();

        Assert.That(async () => await repository.SelectCurrentAsync(responseId, attempt.Id), Throws.TypeOf<EntityConflictException>());
        Assert.That(await context.TestResponses.AsNoTracking().Where(e => e.Id == responseId).Select(e => e.CurrentEvaluationAttemptId).SingleAsync(), Is.Null);
    }

    private async Task<int> CreateSubjectAsync(SubjectIndexAvailability availability = SubjectIndexAvailability.Ready)
    {
        await using var context = CreateContext();
        var subject = context.Subjects.Add(new Subject { Code = $"EXP-{Guid.NewGuid():N}"[..20], Name = "Experiment integration", IndexAvailability = availability }).Entity;
        await context.SaveChangesAsync();
        return subject.Id;
    }

    private async Task<Guid> CreateAnsweredResponseAsync()
    {
        var subjectId = await CreateSubjectAsync();
        var experiment = NewExperiment(subjectId, "Evaluation attempts");
        var response = new TestResponse
        {
            Id = Guid.NewGuid(), ExperimentId = experiment.Id, SourceQuestionId = 1, Status = ExperimentQuestionStatus.Completed, ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            DatasetName = "Dataset", DatasetKey = "dataset-v1", DatasetVersion = "1", QuestionExternalId = "Q-001", QuestionLanguage = "vi", Question = "Question", GroundTruth = "Reference", GeneratedAnswer = "Answer",
        };
        experiment.TestResponses.Add(response);
        await using var context = CreateContext();
        context.Experiments.Add(experiment);
        await context.SaveChangesAsync();
        return response.Id;
    }

    private static TestResponseEvaluationAttempt Attempt() => new()
    {
        EvaluatorFamily = "python-ragas", ContractVersion = "ragas-evaluation-v1", ServiceVersion = "1.0.0", RagasVersion = "0.4.3", PromptVersion = "vi-ragas-v1", Language = "vi",
        LlmProvider = "ollama", LlmModel = "qwen3", EmbeddingProvider = "ollama", EmbeddingModel = "bge-m3", MetricSetKey = "ragas-rag-core-v1", EvaluatorProfileKey = "profile",
    };

    private static Experiment NewExperiment(int subjectId, string name)
    {
        var id = Guid.NewGuid();

        return new Experiment
        {
            Id = id,
            ExperimentName = name,
            SubjectId = subjectId,
            Status = ExperimentStatus.Queued,
            QuestionSetKey = "integration-set",
            TotalQuestionCount = 1,
            ConfigurationSnapshot = new ExperimentConfigurationSnapshot
            {
                Id = id,
                SubjectId = subjectId,
                SubjectCode = "TEST",
                SubjectName = "Experiment integration",
                ChunkingStrategy = ChunkingStrategy.RecursiveSeparator,
                ChunkSize = 1200,
                ChunkOverlap = 200,
                EmbeddingModel = EmbeddingModelName.BgeM3,
                TopK = 8,
                SimilarityThreshold = .5,
                MaxContextChunks = 5,
                LlmModel = ChatModelName.Qwen3,
                ChatTemperature = .2F,
                MaxHistoryMessages = 10,
                ChatPrompt = "Answer using context.",
                ContextPrompt = "Context:",
                NoContextRetrievedPrompt = "No context.",
                CitationExtractionTemperature = 0,
                CitationExtractionPrompt = "Extract citations.",
                EvaluatorLlmProvider = "gemini", EvaluatorLlmModel = ChatModelName.Gemini35Flash,
                EvaluatorEmbeddingProvider = "gemini", EvaluatorEmbeddingModel = EmbeddingModelName.GeminiEmbedding2,
                EvaluatorMetricSetKey = "ragas-rag-core-v1", EvaluatorPromptVersion = "vi-ragas-v1",
            },
        };
    }

    private static SubjectAiConfiguration Configuration(int subjectId) => new()
    {
        Id = subjectId,
        ChunkingStrategy = ChunkingStrategy.RecursiveSeparator,
        ChunkSize = 1200,
        ChunkOverlap = 200,
        EmbeddingModel = EmbeddingModelName.BgeM3,
        TopK = 8,
        SimilarityThreshold = .5,
        MaxContextChunks = 5,
        LlmModel = ChatModelName.Qwen3,
        ChatTemperature = .2F,
        MaxHistoryMessages = 10,
        ChatPrompt = "Answer using context.",
        ContextPrompt = "Context:",
        NoContextRetrievedPrompt = "No context.",
        TitleTemperature = .2F,
        TitlePrompt = "Generate a title.",
        CitationExtractionTemperature = 0,
        CitationExtractionPrompt = "Extract citations.",
    };

    private static async Task<Exception?> AttemptCreateAsync(ExperimentRunRepository repository, Experiment experiment)
    {
        try
        {
            await repository.CreateQueuedAsync(experiment);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private EduChatAiDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<EduChatAiDbContext>().UseNpgsql(_connectionString, npgsql => npgsql.UseVector()).UseSnakeCaseNamingConvention().Options);
}
