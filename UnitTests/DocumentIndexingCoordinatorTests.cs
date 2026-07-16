using Business.Services.AI.Indexing;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pgvector;
using System.Linq.Expressions;

namespace UnitTests;

[TestFixture]
public sealed class DocumentIndexingCoordinatorTests
{
    [Test]
    public async Task IndexAsync_NewDocument_UsesOneConfigurationAndPersistsSuccessfulIdentity()
    {
        var f = new Fixture();
        ChunkingOptions? usedOptions = null;
        f.Chunker.Setup(e => e.Chunk(It.IsAny<IReadOnlyList<ParsedSection>>(), It.IsAny<ChunkingOptions>(), It.IsAny<int>()))
            .Callback<IReadOnlyList<ParsedSection>, ChunkingOptions, int>((_, options, _) => usedOptions = options)
            .Returns([new ChunkResult { ChunkIndex = 0, ChunkText = "chunk", StartPageNumber = 1, EndPageNumber = 1 }]);

        await f.Create().IndexAsync(f.Document.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Document.Status, Is.EqualTo(DocumentStatus.Indexed));
            Assert.That(f.Document.IndexedChunkingStrategy, Is.EqualTo(f.Config.ChunkingStrategy));
            Assert.That(f.Document.IndexedChunkSize, Is.EqualTo(f.Config.ChunkSize));
            Assert.That(f.Document.IndexedChunkOverlap, Is.EqualTo(f.Config.ChunkOverlap));
            Assert.That(f.Document.IndexedEmbeddingModel, Is.EqualTo(f.Config.EmbeddingModel));
            Assert.That(usedOptions, Is.EqualTo(new ChunkingOptions(f.Config.ChunkSize, f.Config.ChunkOverlap)));
            Assert.That(f.ChunkRows, Has.Count.EqualTo(1));
            Assert.That(f.ChunkRows[0].Embedding, Is.Not.Null);
        }

        f.Resolver.Verify(e => e.GetAiConfigurationAsync(f.Document.SubjectId, It.IsAny<CancellationToken>()), Times.Once);
        f.Embedder.Verify(e => e.EmbedAsync(It.IsAny<IEnumerable<string>>(), f.Config.EmbeddingModel, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IndexAsync_CompatibleCompletedDocument_DoesNoWork()
    {
        var f = new Fixture();
        f.Document.Status = DocumentStatus.Indexed;
        f.Document.IndexedChunkingStrategy = f.Config.ChunkingStrategy;
        f.Document.IndexedChunkSize = f.Config.ChunkSize;
        f.Document.IndexedChunkOverlap = f.Config.ChunkOverlap;
        f.Document.IndexedEmbeddingModel = f.Config.EmbeddingModel;
        f.ChunkRows.Add(new Chunk
        {
            DocumentId = f.Document.Id,
            ChunkIndex = 0,
            ChunkText = "chunk",
            ChunkingStrategy = f.Config.ChunkingStrategy,
            EmbeddingModel = f.Config.EmbeddingModel,
            Embedding = new Vector(new float[1024]),
        });

        await f.Create().IndexAsync(f.Document.Id);

        f.Parser.Verify(e => e.ParseAsync(It.IsAny<Stream>(), It.IsAny<FileType>(), It.IsAny<CancellationToken>()), Times.Never);
        f.Selector.Verify(e => e.Select(It.IsAny<string>()), Times.Never);
        f.Embedder.Verify(e => e.EmbedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        f.Files.Verify(e => e.MoveAsync(It.IsAny<Guid>(), It.IsAny<FileDirectoryCategory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void IndexAsync_EmbeddingFails_DoesNotOverwritePreviousSuccessfulIdentity()
    {
        var f = new Fixture();
        f.Document.IndexedChunkingStrategy = "OldStrategy";
        f.Document.IndexedChunkSize = 300;
        f.Document.IndexedChunkOverlap = 30;
        f.Document.IndexedEmbeddingModel = "old-model";
        f.SectionRows.Add(new ParsedSection { DocumentId = f.Document.Id, SectionIndex = 0, Text = "source" });
        f.Embedder.Setup(e => e.EmbedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Embedding failed."));

        Assert.ThrowsAsync<InvalidOperationException>(() => f.Create().IndexAsync(f.Document.Id));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Document.Status, Is.EqualTo(DocumentStatus.Failed));
            Assert.That(f.Document.IndexedChunkingStrategy, Is.EqualTo("OldStrategy"));
            Assert.That(f.Document.IndexedChunkSize, Is.EqualTo(300));
            Assert.That(f.Document.IndexedChunkOverlap, Is.EqualTo(30));
            Assert.That(f.Document.IndexedEmbeddingModel, Is.EqualTo("old-model"));
            Assert.That(f.ChunkRows, Has.Count.EqualTo(1));
            Assert.That(f.ChunkRows[0].Embedding, Is.Null);
        }
    }

    private sealed class Fixture
    {
        public Document Document { get; } = new()
        {
            Id = Guid.NewGuid(),
            SubjectId = 1,
            FileType = FileType.PDF,
            StorageMethod = FileStorageMethod.LocalHardDrive,
            StorageLocator = "received/document.pdf",
            Status = DocumentStatus.Received,
        };

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

        public List<ParsedSection> SectionRows { get; } = [];
        public List<Chunk> ChunkRows { get; } = [];

        public Mock<IDocumentParser> Parser { get; } = new();
        public Mock<IDocumentChunker> Chunker { get; } = new();
        public Mock<IDocumentChunkerSelector> Selector { get; } = new();
        public Mock<IEmbeddingService> Embedder { get; } = new();
        public Mock<IDocumentFileService> Files { get; } = new();
        public Mock<IAiConfigurationResolver> Resolver { get; } = new();
        public Mock<IDocumentStatusRealtimeNotifier> Notifier { get; } = new();
        public Mock<ILogger<SingleRunDocumentIndexingCoordinator>> Logger { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        private readonly Mock<GenericRepository<Document>> _documents = Repository<Document>();
        private readonly Mock<GenericRepository<ParsedSection>> _sections = Repository<ParsedSection>();
        private readonly Mock<GenericRepository<Chunk>> _chunks = Repository<Chunk>();

        public Fixture()
        {
            Parser.SetupGet(e => e.ParserName).Returns("Test parser");
            Parser.Setup(e => e.ParseAsync(It.IsAny<Stream>(), It.IsAny<FileType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParsedDocument { Sections = [new ParsedSection { SectionIndex = 0, PageNumber = 1, Text = "source" }] });

            Chunker.SetupGet(e => e.StrategyName).Returns(ChunkingStrategy.FixedLength);
            Chunker.Setup(e => e.Chunk(It.IsAny<IReadOnlyList<ParsedSection>>(), It.IsAny<ChunkingOptions>(), It.IsAny<int>()))
                .Returns([new ChunkResult { ChunkIndex = 0, ChunkText = "chunk", StartPageNumber = 1, EndPageNumber = 1 }]);
            Selector.Setup(e => e.Select(Config.ChunkingStrategy)).Returns(Chunker.Object);

            Resolver.Setup(e => e.GetAiConfigurationAsync(Document.SubjectId, It.IsAny<CancellationToken>())).ReturnsAsync(Config);
            Embedder.Setup(e => e.EmbedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<IEnumerable<string>, string, CancellationToken>((texts, model, _) => Task.FromResult(new EmbedResult
                {
                    Model = model,
                    Vectors = texts.Select(_ => new ReadOnlyMemory<float>(new float[1024])).ToList(),
                }));

            Files.Setup(e => e.OpenReadAsync(Document.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new FileReadResult { Success = true, FileStream = new MemoryStream([1]) });
            Files.Setup(e => e.MoveAsync(Document.Id, It.IsAny<FileDirectoryCategory>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileStorageResult { Success = true, Locator = "moved/document.pdf" });

            Notifier.Setup(e => e.PushUpdateAsync(It.IsAny<DocumentStatusUpdate>())).Returns(Task.CompletedTask);

            _documents.Setup(e => e.FindByIdAsync(Document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Document);
            SetupGet(_sections, () => SectionRows);
            SetupGet(_chunks, () => ChunkRows);
            _sections.Setup(e => e.Insert(It.IsAny<ParsedSection>())).Returns<ParsedSection>(e => { SectionRows.Add(e); return e; });
            _chunks.Setup(e => e.Insert(It.IsAny<Chunk>())).Returns<Chunk>(e => { ChunkRows.Add(e); return e; });
            _chunks.Setup(e => e.Delete(It.IsAny<Chunk>())).Returns<Chunk>(e => { ChunkRows.Remove(e); return e; });
            _chunks.Setup(e => e.Update(It.IsAny<Chunk>())).Returns<Chunk>(e => e);

            UnitOfWork.SetupGet(e => e.Documents).Returns(_documents.Object);
            UnitOfWork.SetupGet(e => e.ParsedSections).Returns(_sections.Object);
            UnitOfWork.SetupGet(e => e.Chunks).Returns(_chunks.Object);
            UnitOfWork.Setup(e => e.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        public SingleRunDocumentIndexingCoordinator Create() => new(Parser.Object, Selector.Object, Embedder.Object, Files.Object, Resolver.Object, UnitOfWork.Object, Notifier.Object, Logger.Object);

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
