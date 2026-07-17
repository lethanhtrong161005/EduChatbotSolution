using Domain.Contracts;
using Domain.Contracts.DTOs;
using Moq;
using NUnit.Framework;
using Presentation.Background;

namespace UnitTests;

[TestFixture]
public sealed class UserImportBackgroundJobTests
{
    [Test]
    public async Task FilePersistenceJob_PersistSucceeds_Completes()
    {
        var batchId = Guid.NewGuid();
        var files = new Mock<IUserImportFileService>();
        files.Setup(e => e.PersistAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = true, Locator = $"user_imports/received/{batchId}.xlsx" });

        await new UserImportFilePersistenceJob(files.Object).PersistAsync(batchId, CancellationToken.None);

        files.Verify(e => e.PersistAsync(batchId, CancellationToken.None), Times.Once);
    }

    [Test]
    public void FilePersistenceJob_PersistFails_ThrowsForHangfireRetry()
    {
        var batchId = Guid.NewGuid();
        var files = new Mock<IUserImportFileService>();
        files.Setup(e => e.PersistAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = false, Errors = ["upload failed", "retry later"] });

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => new UserImportFilePersistenceJob(files.Object).PersistAsync(batchId, CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain("upload failed").And.Contain("retry later"));
    }

    [Test]
    public async Task UserImportJob_DelegatesToCoordinator()
    {
        var batchId = Guid.NewGuid();
        var coordinator = new Mock<IUserImportCoordinator>();
        coordinator.Setup(e => e.ImportAsync(batchId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await new UserImportJob(coordinator.Object).ImportAsync(batchId, CancellationToken.None);

        coordinator.Verify(e => e.ImportAsync(batchId, CancellationToken.None), Times.Once);
    }

    [Test]
    public void UserImportJob_CoordinatorFails_PropagatesForHangfireRetry()
    {
        var batchId = Guid.NewGuid();
        var coordinator = new Mock<IUserImportCoordinator>();
        coordinator.Setup(e => e.ImportAsync(batchId, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("import failed"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => new UserImportJob(coordinator.Object).ImportAsync(batchId, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("import failed"));
    }
}
