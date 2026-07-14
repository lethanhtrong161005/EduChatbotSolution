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

    private async Task<int> CreateSubjectAsync(SubjectIndexAvailability availability = SubjectIndexAvailability.Ready)
    {
        await using var context = CreateContext();
        var subject = context.Subjects.Add(new Subject { Code = $"EXP-{Guid.NewGuid():N}"[..20], Name = "Experiment integration", IndexAvailability = availability }).Entity;
        await context.SaveChangesAsync();
        return subject.Id;
    }

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
                JudgeModel = ChatModelName.Gemini35Flash,
                EvaluatorPromptVersion = "ragas-style-v1",
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
