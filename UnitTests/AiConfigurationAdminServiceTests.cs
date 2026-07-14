using Business.Services.AI;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;

namespace UnitTests;

[TestFixture]
public sealed class AiConfigurationAdminServiceTests
{
    [Test]
    public async Task GetSubjectConfiguration_SeparatesGlobalOverrideAndEffectiveValues()
    {
        var f = new Fixture();
        f.Stored.ChunkSize = 600;
        f.Stored.ChatPrompt = "Subject prompt";

        var result = await f.Create().GetSubjectConfigurationAsync(f.Subject.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Indexing.ChunkSize.GlobalDefault, Is.EqualTo(f.Global.ChunkSize));
            Assert.That(result.Indexing.ChunkSize.StoredOverride, Is.EqualTo(600));
            Assert.That(result.Indexing.ChunkSize.EffectiveValue, Is.EqualTo(600));
            Assert.That(result.Prompts.ChatPrompt.GlobalDefault, Is.EqualTo(f.Global.ChatPrompt));
            Assert.That(result.Prompts.ChatPrompt.StoredOverride, Is.EqualTo("Subject prompt"));
            Assert.That(result.Prompts.ChatPrompt.EffectiveValue, Is.EqualTo("Subject prompt"));
        }
    }

    [Test]
    public async Task SaveSubjectConfiguration_NullsRemoveOverrides()
    {
        var f = new Fixture();
        f.Stored.ChunkSize = 600;
        f.Stored.ChatPrompt = "Subject prompt";

        var result = await f.Create().SaveSubjectConfigurationAsync(f.Subject.Id, new SaveSubjectAiConfigurationRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Stored.ChunkSize, Is.Null);
            Assert.That(f.Stored.ChatPrompt, Is.Null);
            Assert.That(result.Indexing.ChunkSize.StoredOverride, Is.Null);
            Assert.That(result.Indexing.ChunkSize.EffectiveValue, Is.EqualTo(f.Global.ChunkSize));
            Assert.That(result.Prompts.ChatPrompt.EffectiveValue, Is.EqualTo(f.Global.ChatPrompt));
        }

        f.UnitOfWork.Verify(e => e.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void SaveSubjectConfiguration_OverlapNotBelowEffectiveSize_ThrowsValidation()
    {
        var f = new Fixture();

        var exception = Assert.ThrowsAsync<EntityValidationException>(() =>
            f.Create().SaveSubjectConfigurationAsync(f.Subject.Id, new SaveSubjectAiConfigurationRequest { ChunkSize = 100, ChunkOverlap = 100 }));

        Assert.That(exception!.Property, Is.EqualTo(nameof(SaveSubjectAiConfigurationRequest.ChunkOverlap)));
    }

    [Test]
    public async Task ReindexSubject_AcquiresGateAndQueuesResolvedSnapshot()
    {
        var f = new Fixture();
        f.Documents.Setup(e => e.CountAsync(It.IsAny<Expression<Func<Document, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await f.Create().ReindexSubjectAsync(f.Subject.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SubjectId, Is.EqualTo(f.Subject.Id));
            Assert.That(result.QueuedDocumentCount, Is.EqualTo(2));
            Assert.That(result.QueuedAt, Is.Not.Default);
        }

        f.SubjectIndexes.Verify(e => e.AcquireExclusiveReindexAsync(f.Subject.Id, It.IsAny<CancellationToken>()), Times.Once);
        f.Dispatcher.Verify(e => e.Enqueue(f.Subject.Id, f.Effective), Times.Once);
    }

    [Test]
    public void ReindexSubject_ConcurrentReindex_PropagatesConflictWithoutQueueing()
    {
        var f = new Fixture();
        f.SubjectIndexes.Setup(e => e.AcquireExclusiveReindexAsync(f.Subject.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConflictException("Already reindexing."));

        Assert.ThrowsAsync<EntityConflictException>(() => f.Create().ReindexSubjectAsync(f.Subject.Id));
        f.Dispatcher.Verify(e => e.Enqueue(It.IsAny<int>(), It.IsAny<EffectiveAiConfiguration>()), Times.Never);
    }

    private sealed class Fixture
    {
        public Subject Subject { get; } = new() { Id = 7, Code = "DB201", Name = "Database Systems" };
        public GlobalAiConfiguration Global { get; } = new();
        public SubjectAiConfiguration Stored { get; }
        public EffectiveAiConfiguration Effective { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IAiConfigurationResolver> Resolver { get; } = new();
        public Mock<ISubjectReindexDispatcher> Dispatcher { get; } = new();
        public Mock<SubjectIndexRepository> SubjectIndexes { get; } = new();

        public Mock<GenericRepository<Subject>> Subjects { get; } = Repository<Subject>();
        public Mock<GenericRepository<GlobalAiConfiguration>> Globals { get; } = Repository<GlobalAiConfiguration>();
        public Mock<GenericRepository<SubjectAiConfiguration>> Configurations { get; } = Repository<SubjectAiConfiguration>();
        public Mock<GenericRepository<Document>> Documents { get; } = Repository<Document>();

        public Fixture()
        {
            Stored = new SubjectAiConfiguration { Id = Subject.Id };
            Effective = ToEffective(Global);

            SetupGet(Subjects, () => [Subject]);
            SetupGet(Globals, () => [Global]);

            Subjects.Setup(e => e.FindByIdAsync(Subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Subject);
            Configurations.Setup(e => e.FindByIdAsync(Subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Stored);
            Configurations.Setup(e => e.Insert(It.IsAny<SubjectAiConfiguration>())).Returns<SubjectAiConfiguration>(e => e);
            SubjectIndexes.Setup(e => e.AcquireExclusiveReindexAsync(Subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(SubjectIndexAvailability.Ready);
            SubjectIndexes.Setup(e => e.SetAvailabilityAsync(It.IsAny<int>(), It.IsAny<SubjectIndexAvailability>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            UnitOfWork.SetupGet(e => e.Subjects).Returns(Subjects.Object);
            UnitOfWork.SetupGet(e => e.GlobalAiConfigurations).Returns(Globals.Object);
            UnitOfWork.SetupGet(e => e.SubjectAiConfigurations).Returns(Configurations.Object);
            UnitOfWork.SetupGet(e => e.Documents).Returns(Documents.Object);
            UnitOfWork.SetupGet(e => e.SubjectIndexes).Returns(SubjectIndexes.Object);
            UnitOfWork.Setup(e => e.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            Resolver.Setup(e => e.GetAiConfigurationAsync(Subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Effective);
            Dispatcher.Setup(e => e.Enqueue(Subject.Id, Effective)).Returns("job-id");
        }

        public AiConfigurationAdminService Create() => new(Resolver.Object, UnitOfWork.Object, Dispatcher.Object);

        private static EffectiveAiConfiguration ToEffective(GlobalAiConfiguration g) => new()
        {
            ChunkingStrategy = g.ChunkingStrategy,
            ChunkSize = g.ChunkSize,
            ChunkOverlap = g.ChunkOverlap,
            EmbeddingModel = g.EmbeddingModel,
            TopK = g.TopK,
            SimilarityThreshold = g.SimilarityThreshold,
            LlmModel = g.LlmModel,
            ChatTemperature = g.ChatTemperature,
            ChatPrompt = g.ChatPrompt,
            ContextPrompt = g.ContextPrompt,
            NoContextRetrievedPrompt = g.NoContextRetrievedPrompt,
            TitleTemperature = g.TitleTemperature,
            TitlePrompt = g.TitlePrompt,
            CitationExtractionTemperature = g.CitationExtractionTemperature,
            CitationExtractionPrompt = g.CitationExtractionPrompt,
            MaxContextChunks = g.MaxContextChunks,
            MaxHistoryMessages = g.MaxHistoryMessages,
        };

        private static Mock<GenericRepository<T>> Repository<T>() where T : class
        {
            var set = new Mock<DbSet<T>>();
            var context = new Mock<DbContext>();
            context.Setup(e => e.Set<T>()).Returns(set.Object);
            return new Mock<GenericRepository<T>>(context.Object);
        }

        private static void SetupGet<T>(Mock<GenericRepository<T>> repository, Func<IEnumerable<T>> rows) where T : class =>
            repository.Setup(e => e.GetAsync(
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Func<IQueryable<T>, IOrderedQueryable<T>>>(),
                It.IsAny<(int, int)>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
    }
}