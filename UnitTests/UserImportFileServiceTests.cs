using Business.Services.Account;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Text;

namespace UnitTests;

[TestFixture]
public sealed class UserImportFileServiceTests
{
    private Mock<IStagingFileStore> _staging = null!;
    private Mock<IDurableStorageStrategy> _local = null!;
    private Mock<IDurableStorageStrategy> _supabase = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<GenericRepository<UserImportBatch>> _batches = null!;

    [SetUp]
    public void SetUp()
    {
        _staging = new Mock<IStagingFileStore>();
        _local = Strategy(FileStorageMethod.LocalHardDrive);
        _supabase = Strategy(FileStorageMethod.Supabase);
        _unitOfWork = new Mock<IUnitOfWork>();
        _batches = Repository<UserImportBatch>();

        _unitOfWork.SetupGet(e => e.UserImportBatches).Returns(_batches.Object);
    }

    [Test]
    public async Task PersistAsync_ValidStagedFile_StoresInUserImportBucketAndCommitsLocator()
    {
        var batch = NewBatch("Import.USERS.XLSX", "staging/source");
        SetBatch(batch);

        _staging.Setup(e => e.OpenReadAsync("staging/source", It.IsAny<CancellationToken>())).ReturnsAsync(ReadSuccess("xlsx"));
        _supabase.Setup(e => e.StoreAsync(It.IsAny<Stream>(), FileResourceType.UserImportBatch, $"{batch.Id}.xlsx", FileDirectoryCategory.Received, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = true, Locator = $"user_imports/received/{batch.Id}.xlsx" });
        _staging.Setup(e => e.DeleteAsync("staging/source", CancellationToken.None)).ReturnsAsync(new FileDeletionResult { Success = true });

        var result = await CreateService().PersistAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Locator, Is.EqualTo($"user_imports/received/{batch.Id}.xlsx"));
            Assert.That(batch.StorageLocator, Is.EqualTo(result.Locator));
            Assert.That(batch.StagingLocator, Is.Null);
        }

        _unitOfWork.Verify(e => e.SaveAsync(CancellationToken.None), Times.Once);
        _staging.Verify(e => e.DeleteAsync("staging/source", CancellationToken.None), Times.Once);
        _local.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PersistAsync_DurableStoreFails_PreservesStagingState()
    {
        var batch = NewBatch("users.xlsx", "staging/source");
        SetBatch(batch);

        _staging.Setup(e => e.OpenReadAsync("staging/source", It.IsAny<CancellationToken>())).ReturnsAsync(ReadSuccess("xlsx"));
        _supabase.Setup(e => e.StoreAsync(It.IsAny<Stream>(), FileResourceType.UserImportBatch, $"{batch.Id}.xlsx", FileDirectoryCategory.Received, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = false, Errors = ["storage unavailable"] });

        var result = await CreateService().PersistAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Does.Contain("storage unavailable"));
            Assert.That(batch.StagingLocator, Is.EqualTo("staging/source"));
            Assert.That(batch.StorageLocator, Is.Null);
        }

        _unitOfWork.Verify(e => e.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _staging.Verify(e => e.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PersistAsync_StagingCleanupThrows_DurableCommitStillSucceeds()
    {
        var batch = NewBatch("users.xlsx", "staging/source");
        SetBatch(batch);

        _staging.Setup(e => e.OpenReadAsync("staging/source", It.IsAny<CancellationToken>())).ReturnsAsync(ReadSuccess("xlsx"));
        _supabase.Setup(e => e.StoreAsync(It.IsAny<Stream>(), FileResourceType.UserImportBatch, $"{batch.Id}.xlsx", FileDirectoryCategory.Received, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStorageResult { Success = true, Locator = $"user_imports/received/{batch.Id}.xlsx" });
        _staging.Setup(e => e.DeleteAsync("staging/source", CancellationToken.None)).ThrowsAsync(new IOException("cleanup failed"));

        var result = await CreateService().PersistAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(batch.StorageLocator, Is.EqualTo($"user_imports/received/{batch.Id}.xlsx"));
            Assert.That(batch.StagingLocator, Is.Null);
        }

        _unitOfWork.Verify(e => e.SaveAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task PersistAsync_MissingStagingLocator_ReturnsFailureWithoutStorageCalls()
    {
        var batch = NewBatch("users.xlsx", null);
        SetBatch(batch);

        var result = await CreateService().PersistAsync(batch.Id, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors.Single(), Does.Contain("no staged file").IgnoreCase);
        _staging.VerifyNoOtherCalls();
        _supabase.VerifyNoOtherCalls();
        _local.VerifyNoOtherCalls();
        _unitOfWork.Verify(e => e.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OpenReadAsync_PersistedBatch_RoutesStoredLocatorToSupabase()
    {
        var batch = NewBatch("users.xlsx", null);
        batch.StorageLocator = $"user_imports/received/{batch.Id}.xlsx";
        SetBatch(batch);

        var expected = ReadSuccess("xlsx");
        _supabase.Setup(e => e.OpenReadAsync(batch.StorageLocator, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateService().OpenReadAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileStream, Is.SameAs(expected.FileStream));
        }

        _local.VerifyNoOtherCalls();
    }

    private UserImportFileService CreateService() => new(_staging.Object, _local.Object, _supabase.Object, _unitOfWork.Object);

    private void SetBatch(UserImportBatch batch) =>
        _batches.Setup(e => e.FindByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

    private static UserImportBatch NewBatch(string fileName, string? stagingLocator) => new()
    {
        Id = Guid.NewGuid(),
        FileName = fileName,
        StagingLocator = stagingLocator,
        ImportedById = Guid.NewGuid(),
        Status = ImportBatchStatus.Pending,
    };

    private static Mock<IDurableStorageStrategy> Strategy(FileStorageMethod method)
    {
        var strategy = new Mock<IDurableStorageStrategy>();
        strategy.SetupGet(e => e.Method).Returns(method);
        return strategy;
    }

    private static FileReadResult ReadSuccess(string content) => new()
    {
        Success = true,
        FileStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
    };

    private static Mock<GenericRepository<T>> Repository<T>() where T : class
    {
        var set = new Mock<DbSet<T>>();
        var context = new Mock<DbContext>();
        context.Setup(e => e.Set<T>()).Returns(set.Object);
        return new Mock<GenericRepository<T>>(context.Object);
    }
}
