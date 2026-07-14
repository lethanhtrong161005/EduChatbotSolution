using Business.Services.AI.Indexing;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace UnitTests;

[TestFixture]
public sealed class SubjectReindexCoordinatorTests
{
    [Test]
    public async Task RunAsync_ReindexingSubject_IndexesEveryDocumentWithSnapshotAndMarksReady()
    {
        var f = new Fixture();

        await f.Create().RunAsync(f.Subject.Id, f.Config);

        foreach (var document in f.DocumentRows)
            f.Indexer.Verify(e => e.IndexAsync(document.Id, f.Config, It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(f.SubjectIndexes.SetCalls, Is.EqualTo(new[] { (f.Subject.Id, SubjectIndexAvailability.Ready) }));
    }

    [Test]
    public void RunAsync_DocumentIndexingFails_MarksSubjectFailedAndRethrows()
    {
        var f = new Fixture();
        f.Indexer.Setup(e => e.IndexAsync(f.DocumentRows[0].Id, f.Config, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Index failure."));

        Assert.ThrowsAsync<InvalidOperationException>(() => f.Create().RunAsync(f.Subject.Id, f.Config));

        Assert.That(f.SubjectIndexes.SetCalls, Is.EqualTo(new[] { (f.Subject.Id, SubjectIndexAvailability.Failed) }));
    }

    [Test]
    public async Task RunAsync_SubjectIsNotReindexing_DoesNothing()
    {
        var f = new Fixture();
        f.Subject.IndexAvailability = SubjectIndexAvailability.Ready;

        await f.Create().RunAsync(f.Subject.Id, f.Config);

        f.Indexer.Verify(e => e.IndexAsync(It.IsAny<Guid>(), It.IsAny<EffectiveAiConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(f.SubjectIndexes.SetCalls, Is.Empty);
    }

    private sealed class Fixture
    {
        public Subject Subject { get; } = new() { Id = 7, Code = "DB201", Name = "Database Systems", IndexAvailability = SubjectIndexAvailability.Reindexing };
        public List<Document> DocumentRows { get; } =
        [
            new() { Id = Guid.NewGuid(), SubjectId = 7, UploadedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { Id = Guid.NewGuid(), SubjectId = 7, UploadedAt = DateTime.UtcNow.AddMinutes(-1) },
        ];

        public EffectiveAiConfiguration Config { get; } = new()
        {
            ChunkingStrategy = ChunkingStrategy.FixedLength,
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = EmbeddingModelName.BgeM3,
            TopK = 5,
            SimilarityThreshold = .5,
            LlmModel = ChatModelName.Qwen3,
            ChatTemperature = 0,
            ChatPrompt = "",
            ContextPrompt = "",
            NoContextRetrievedPrompt = "",
            TitleTemperature = 0,
            TitlePrompt = "",
            CitationExtractionTemperature = 0,
            CitationExtractionPrompt = "",
            MaxContextChunks = 5,
            MaxHistoryMessages = 10,
        };

        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public Mock<IDocumentIndexingCoordinator> Indexer { get; } = new();
        public Mock<ILogger<SubjectReindexCoordinator>> Logger { get; } = new();
        public StubSubjectIndexRepository SubjectIndexes { get; } = new();

        private readonly Mock<GenericRepository<Subject>> _subjects = Repository<Subject>();
        private readonly Mock<GenericRepository<Document>> _documents = Repository<Document>();

        public Fixture()
        {
            _subjects.Setup(e => e.FindByIdAsync(Subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Subject);
            SetupGet(_documents, () => DocumentRows);
            UnitOfWork.SetupGet(e => e.Subjects).Returns(_subjects.Object);
            UnitOfWork.SetupGet(e => e.Documents).Returns(_documents.Object);
            UnitOfWork.SetupGet(e => e.SubjectIndexes).Returns(SubjectIndexes);
            Indexer.Setup(e => e.IndexAsync(It.IsAny<Guid>(), It.IsAny<EffectiveAiConfiguration>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        public SubjectReindexCoordinator Create() => new(UnitOfWork.Object, Indexer.Object, Logger.Object);

        private static Mock<GenericRepository<T>> Repository<T>() where T : class
        {
            var set = new Mock<DbSet<T>>();
            var context = new Mock<DbContext>();
            context.Setup(e => e.Set<T>()).Returns(set.Object);
            return new Mock<GenericRepository<T>>(context.Object);
        }

        private static void SetupGet<T>(Mock<GenericRepository<T>> repository, Func<IEnumerable<T>> rows) where T : class =>
            repository.Setup(e => e.GetAsync(It.IsAny<string[]>(), It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Func<IQueryable<T>, IOrderedQueryable<T>>>(), It.IsAny<(int, int)>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);
    }

    private sealed class StubSubjectIndexRepository : SubjectIndexRepository
    {
        public List<(int SubjectId, SubjectIndexAvailability Availability)> SetCalls { get; } = [];

        public StubSubjectIndexRepository() : base(null!) { }

        public override Task SetAvailabilityAsync(int subjectId, SubjectIndexAvailability availability, CancellationToken cxlTkn = default)
        {
            SetCalls.Add((subjectId, availability));
            return Task.CompletedTask;
        }
    }
}
