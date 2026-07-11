using Domain.Contracts;
using Domain.Contracts.DTOs;
using Moq;
using Presentation.Background;

namespace UnitTests;

public class DocumentPersistenceJobTests
{
    [Test]
    public void PersistAsync_ThrowsWhenTypedPersistenceResultFails()
    {
        var documentId = Guid.NewGuid();
        var fileService = new Mock<IDocumentFileService>();
        fileService.Setup(x => x.PersistAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileLocatorResult { Success = false, Errors = ["store failed"] });
        var job = new DocumentPersistenceJob(fileService.Object);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => job.PersistAsync(documentId));

        Assert.That(exception!.Message, Does.Contain("store failed"));
    }
}
