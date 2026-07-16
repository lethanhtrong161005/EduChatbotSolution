using Business.Services.Documents.File;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Moq;

namespace UnitTests;

public class DocumentFileServiceTests
{
    private readonly Guid _documentId = Guid.NewGuid();
    private Mock<IDocumentService> _documentService = null!;
    private Mock<IDocumentStorageMethodResolver> _resolver = null!;
    private Mock<IStagingFileStore> _stagingStore = null!;
    private Mock<IDurableStorageStrategy> _localStrategy = null!;
    private Mock<IDurableStorageStrategy> _supabaseStrategy = null!;

    [SetUp]
    public void SetUp()
    {
        _documentService = new Mock<IDocumentService>();
        _resolver = new Mock<IDocumentStorageMethodResolver>();
        _stagingStore = new Mock<IStagingFileStore>();
        _localStrategy = CreateStrategy(FileStorageMethod.LocalHardDrive);
        _supabaseStrategy = CreateStrategy(FileStorageMethod.Supabase);
    }

    [Test]
    public async Task PersistAsync_StoresDeterministicNameAndCommitsDurableState()
    {
        var document = NewDocument(stagingLocator: "staging/source");
        SetDocument(document);
        _resolver.Setup(x => x.ResolveForPersistenceAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FileStorageMethod.Supabase);
        _stagingStore.Setup(x => x.OpenReadAsync("staging/source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ReadSuccess("content"));
        _supabaseStrategy.Setup(x => x.StoreAsync(
                It.IsAny<Stream>(),
                FileResourceType.Document,
                $"{_documentId}.pdf",
                FileDirectoryCategory.Received,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = true, Locator = "uploaded/document.pdf" });
        _stagingStore.Setup(x => x.DeleteAsync("staging/source", CancellationToken.None))
            .ReturnsAsync(new FileDeletionResult { Success = true });

        var result = await CreateService().PersistAsync(_documentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(document.StorageLocator, Is.EqualTo("uploaded/document.pdf"));
            Assert.That(document.StagingLocator, Is.Null);
            Assert.That(document.StorageMethod, Is.EqualTo(FileStorageMethod.Supabase));
        }
        _documentService.Verify(x => x.GetByIdAsync(
            _documentId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _documentService.Verify(x => x.UpdateAsync(document, It.IsAny<CancellationToken>()), Times.Once);
        _stagingStore.Verify(x => x.DeleteAsync("staging/source", CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task PersistAsync_PreservesStagingStateWhenStoreFails()
    {
        var document = NewDocument(stagingLocator: "staging/source");
        SetDocument(document);
        _resolver.Setup(x => x.ResolveForPersistenceAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FileStorageMethod.LocalHardDrive);
        _stagingStore.Setup(x => x.OpenReadAsync("staging/source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ReadSuccess("content"));
        _localStrategy.Setup(x => x.StoreAsync(
                It.IsAny<Stream>(),
                It.IsAny<FileResourceType>(),
                It.IsAny<string>(),
                It.IsAny<FileDirectoryCategory>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = false, Errors = ["store failed"] });

        var result = await CreateService().PersistAsync(_documentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(document.StorageLocator, Is.Null);
            Assert.That(document.StagingLocator, Is.EqualTo("staging/source"));
            Assert.That(document.StorageMethod, Is.EqualTo(FileStorageMethod.Unspecified));
        }
        _documentService.Verify(x => x.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
        _stagingStore.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PersistAsync_RemainsSuccessfulWhenPostCommitStagingCleanupThrows()
    {
        var document = NewDocument(stagingLocator: "staging/source");
        SetDocument(document);
        _resolver.Setup(x => x.ResolveForPersistenceAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FileStorageMethod.Supabase);
        _stagingStore.Setup(x => x.OpenReadAsync("staging/source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ReadSuccess("content"));
        _supabaseStrategy.Setup(x => x.StoreAsync(
                It.IsAny<Stream>(),
                It.IsAny<FileResourceType>(),
                It.IsAny<string>(),
                It.IsAny<FileDirectoryCategory>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = true, Locator = "uploaded/document.pdf" });
        _stagingStore.Setup(x => x.DeleteAsync("staging/source", CancellationToken.None))
            .ThrowsAsync(new IOException("cleanup failed"));

        var result = await CreateService().PersistAsync(_documentId);

        Assert.That(result.Success, Is.True);
        _documentService.Verify(x => x.UpdateAsync(document, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OpenReadAsync_RoutesUnspecifiedStorageToStaging()
    {
        SetDocument(NewDocument(stagingLocator: "staging/source"));
        _stagingStore.Setup(x => x.OpenReadAsync("staging/source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ReadSuccess("staged"));

        var result = await CreateService().OpenReadAsync(_documentId);

        Assert.That(result.Success, Is.True);
        await result.FileStream!.DisposeAsync();
        _localStrategy.VerifyNoOtherCalls();
        _supabaseStrategy.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OpenReadAsync_RoutesPersistedDocumentByStoredMethod()
    {
        SetDocument(NewDocument("durable/source", FileStorageMethod.LocalHardDrive));
        _localStrategy.Setup(x => x.OpenReadAsync("durable/source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ReadSuccess("durable"));

        var result = await CreateService().OpenReadAsync(_documentId);

        Assert.That(result.Success, Is.True);
        await result.FileStream!.DisposeAsync();
        _localStrategy.Verify(x => x.OpenReadAsync("durable/source", It.IsAny<CancellationToken>()), Times.Once);
        _supabaseStrategy.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OpenReadAsync_FailsForUnknownStorageMethod()
    {
        SetDocument(NewDocument("durable/source", (FileStorageMethod)999));

        var result = await CreateService().OpenReadAsync(_documentId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors.Single(), Does.Contain("Unsupported"));
    }

    [Test]
    public async Task MoveAsync_FailsWhenStorageIsUnspecified()
    {
        SetDocument(NewDocument(stagingLocator: "staging/source"));

        var result = await CreateService().MoveAsync(_documentId, FileDirectoryCategory.Processing);

        Assert.That(result.Success, Is.False);
        _documentService.Verify(x => x.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ClearsDurableStateAfterStrategySucceeds()
    {
        var document = NewDocument("uploaded/document.pdf", FileStorageMethod.Supabase);
        SetDocument(document);
        _supabaseStrategy.Setup(x => x.DeleteAsync("uploaded/document.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileDeletionResult { Success = true });

        var result = await CreateService().DeleteAsync(_documentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(document.StorageLocator, Is.Null);
            Assert.That(document.StorageMethod, Is.EqualTo(FileStorageMethod.Unspecified));
        }
        _documentService.Verify(x => x.UpdateAsync(document, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_PreservesDurableStateWhenStrategyFails()
    {
        var document = NewDocument("uploaded/document.pdf", FileStorageMethod.Supabase);
        SetDocument(document);
        _supabaseStrategy.Setup(x => x.DeleteAsync("uploaded/document.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileDeletionResult { Success = false, Errors = ["delete failed"] });

        var result = await CreateService().DeleteAsync(_documentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(document.StorageLocator, Is.EqualTo("uploaded/document.pdf"));
            Assert.That(document.StorageMethod, Is.EqualTo(FileStorageMethod.Supabase));
        }
        _documentService.Verify(x => x.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private DocumentFileService CreateService() => new(
        _documentService.Object,
        _resolver.Object,
        _stagingStore.Object,
        _localStrategy.Object,
        _supabaseStrategy.Object);

    private static Mock<IDurableStorageStrategy> CreateStrategy(FileStorageMethod method)
    {
        var strategy = new Mock<IDurableStorageStrategy>();
        strategy.SetupGet(x => x.Method).Returns(method);
        return strategy;
    }

    private Document NewDocument(
        string? storageLocator = null,
        FileStorageMethod storageMethod = FileStorageMethod.Unspecified,
        string? stagingLocator = null) => new()
        {
            Id = _documentId,
            FileType = FileType.PDF,
            StorageLocator = storageLocator,
            StagingLocator = stagingLocator,
            StorageMethod = storageMethod,
        };

    private void SetDocument(Document? document)
    {
        _documentService.Setup(x => x.GetByIdAsync(
                _documentId,
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
    }

    private static FileReadResult ReadSuccess(string content) => new()
    {
        Success = true,
        FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
    };
}
