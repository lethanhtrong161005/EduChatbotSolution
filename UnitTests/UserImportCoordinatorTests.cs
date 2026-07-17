using Business.Services.Account;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace UnitTests;

[TestFixture]
public sealed class UserImportCoordinatorTests
{
    private Mock<IUserManagementService> _users = null!;
    private Mock<IUserImportFileService> _files = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IResourceRealtimeNotifier> _notifier = null!;
    private Mock<ILogger<UserImportCoordinator>> _logger = null!;
    private Mock<GenericRepository<UserImportBatch>> _batches = null!;
    private Mock<GenericRepository<UserImportRow>> _rows = null!;

    [SetUp]
    public void SetUp()
    {
        _users = new Mock<IUserManagementService>();
        _files = new Mock<IUserImportFileService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _notifier = new Mock<IResourceRealtimeNotifier>();
        _logger = new Mock<ILogger<UserImportCoordinator>>();
        _batches = Repository<UserImportBatch>();
        _rows = Repository<UserImportRow>();

        _unitOfWork.SetupGet(e => e.UserImportBatches).Returns(_batches.Object);
        _unitOfWork.SetupGet(e => e.UserImportRows).Returns(_rows.Object);
        _notifier.Setup(e => e.PushUpdateAsync(It.IsAny<ResourceUpdate>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task ImportAsync_NewBatch_AllRowsSucceed_CompletesWithExactCounts()
    {
        var batch = NewBatch();
        var parsedRows = new List<UserImportRow>
        {
            NewRow(2, "Second User", "second@example.com"),
            NewRow(1, "First User", "first@example.com"),
        };
        SetBatch(batch);
        _files.Setup(e => e.OpenReadAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ReadSuccess("xlsx"));
        _users.Setup(e => e.ParseImportBatchAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(new UserImportValidationResult(true, [], parsedRows));
        _users.Setup(e => e.CreateUserAsync(It.IsAny<CreateUserDto>())).ReturnsAsync((CreateUserDto dto) => (true, NewUser(dto.FullName, dto.Email), (string?)null));

        await CreateService().ImportAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.Status, Is.EqualTo(ImportBatchStatus.Completed));
            Assert.That(batch.TotalRows, Is.EqualTo(2));
            Assert.That(batch.ProcessedRows, Is.EqualTo(2));
            Assert.That(batch.SuccessRows, Is.EqualTo(2));
            Assert.That(batch.FailedRows, Is.Zero);
            Assert.That(batch.ErrorMessage, Is.Null);
            Assert.That(batch.CompletedAt, Is.Not.Null);
            Assert.That(batch.Rows.Select(e => e.RowNumber), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(batch.Rows.All(e => e.BatchId == batch.Id && e.Status == ImportRowStatus.Success && e.CreatedUserId.HasValue && e.ErrorMessage == null && e.ProcessedAt.HasValue), Is.True);
        }

        _rows.Verify(e => e.Insert(It.IsAny<UserImportRow>()), Times.Exactly(2));
        _users.Verify(e => e.CreateUserAsync(It.IsAny<CreateUserDto>()), Times.Exactly(2));
        _unitOfWork.Verify(e => e.SaveAsync(It.IsAny<CancellationToken>()), Times.AtLeast(5));
    }

    [Test]
    public async Task ImportAsync_PreviouslyFailedRowSucceeds_RecalculatesCountsAndClearsError()
    {
        var batch = NewBatch();

        var completed = NewRow(1, "Completed User", "completed@example.com", ImportRowStatus.Success);
        completed.CreatedUserId = Guid.NewGuid();
        completed.ProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-2);

        var retried = NewRow(2, "Retry User", "retry@example.com", ImportRowStatus.Failed);
        retried.ErrorMessage = "Old failure";
        retried.ProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        batch.Rows.Add(completed);
        batch.Rows.Add(retried);
        batch.TotalRows = 2;
        batch.ProcessedRows = 7;
        batch.SuccessRows = 4;
        batch.FailedRows = 3;
        batch.Status = ImportBatchStatus.Failed;

        SetBatch(batch);
        _users.Setup(e => e.CreateUserAsync(It.Is<CreateUserDto>(x => x.Email == retried.Email))).ReturnsAsync((true, NewUser(retried.FullName, retried.Email), (string?)null));

        await CreateService().ImportAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.Status, Is.EqualTo(ImportBatchStatus.Completed));
            Assert.That(batch.TotalRows, Is.EqualTo(2));
            Assert.That(batch.ProcessedRows, Is.EqualTo(2));
            Assert.That(batch.SuccessRows, Is.EqualTo(2));
            Assert.That(batch.FailedRows, Is.Zero);
            Assert.That(retried.Status, Is.EqualTo(ImportRowStatus.Success));
            Assert.That(retried.CreatedUserId, Is.Not.Null);
            Assert.That(retried.ErrorMessage, Is.Null);
        }

        _users.Verify(e => e.CreateUserAsync(It.Is<CreateUserDto>(x => x.Email == retried.Email)), Times.Once);
        _users.Verify(e => e.CreateUserAsync(It.Is<CreateUserDto>(x => x.Email == completed.Email)), Times.Never);
        _files.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ImportAsync_PreviouslyFailedRowFailsAgain_DoesNotDoubleCountFailure()
    {
        var batch = NewBatch();

        var completed = NewRow(1, "Completed User", "completed@example.com", ImportRowStatus.Success);
        completed.CreatedUserId = Guid.NewGuid();

        var retried = NewRow(2, "Retry User", "retry@example.com", ImportRowStatus.Failed);
        retried.ErrorMessage = "Old failure";

        batch.Rows.Add(completed);
        batch.Rows.Add(retried);
        batch.TotalRows = 2;
        batch.ProcessedRows = 9;
        batch.SuccessRows = 5;
        batch.FailedRows = 4;
        batch.Status = ImportBatchStatus.Failed;

        SetBatch(batch);
        _users.Setup(e => e.CreateUserAsync(It.Is<CreateUserDto>(x => x.Email == retried.Email))).ReturnsAsync((false, null, "Duplicate email."));

        await CreateService().ImportAsync(batch.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.Status, Is.EqualTo(ImportBatchStatus.PartiallyCompleted));
            Assert.That(batch.TotalRows, Is.EqualTo(2));
            Assert.That(batch.ProcessedRows, Is.EqualTo(2));
            Assert.That(batch.SuccessRows, Is.EqualTo(1));
            Assert.That(batch.FailedRows, Is.EqualTo(1));
            Assert.That(retried.Status, Is.EqualTo(ImportRowStatus.Failed));
            Assert.That(retried.ErrorMessage, Is.EqualTo("Duplicate email."));
        }
    }

    [Test]
    public void ImportAsync_DurableFileCannotBeOpened_MarksFailedAndRethrows()
    {
        var batch = NewBatch();
        SetBatch(batch);
        _files.Setup(e => e.OpenReadAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new FileReadResult { Success = false, Errors = ["missing object"] });

        var exception = Assert.ThrowsAsync<FileNotFoundException>(() => CreateService().ImportAsync(batch.Id, CancellationToken.None));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("missing object"));
            Assert.That(batch.Status, Is.EqualTo(ImportBatchStatus.Failed));
            Assert.That(batch.ErrorMessage, Does.Contain("missing object"));
            Assert.That(batch.ProcessedRows, Is.Zero);
            Assert.That(batch.SuccessRows, Is.Zero);
            Assert.That(batch.FailedRows, Is.Zero);
        }

        _unitOfWork.Verify(e => e.SaveAsync(CancellationToken.None), Times.AtLeast(2));
    }

    [Test]
    public async Task ImportAsync_AlreadyComplete_OnlyNormalizesTerminalStatus()
    {
        var batch = NewBatch();

        var first = NewRow(1, "First User", "first@example.com", ImportRowStatus.Success);
        var second = NewRow(2, "Second User", "second@example.com", ImportRowStatus.Success);
        first.CreatedUserId = Guid.NewGuid();
        second.CreatedUserId = Guid.NewGuid();

        batch.Rows.Add(first);
        batch.Rows.Add(second);
        batch.TotalRows = 2;
        batch.ProcessedRows = 2;
        batch.SuccessRows = 2;
        batch.Status = ImportBatchStatus.Processing;

        SetBatch(batch);

        await CreateService().ImportAsync(batch.Id, CancellationToken.None);

        Assert.That(batch.Status, Is.EqualTo(ImportBatchStatus.Completed));
        _unitOfWork.Verify(e => e.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _files.VerifyNoOtherCalls();
        _users.VerifyNoOtherCalls();
    }

    private UserImportCoordinator CreateService() => new(_users.Object, _files.Object, _unitOfWork.Object, _notifier.Object, _logger.Object);

    private void SetBatch(UserImportBatch batch) =>
        _batches.Setup(e => e.GetAsync(
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<UserImportBatch, bool>>>(),
                It.IsAny<Func<IQueryable<UserImportBatch>, IOrderedQueryable<UserImportBatch>>>(),
                It.IsAny<(int, int)>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([batch]);

    private static UserImportBatch NewBatch() => new()
    {
        Id = Guid.NewGuid(),
        FileName = "users.xlsx",
        StorageLocator = "user_imports/received/users.xlsx",
        ImportedById = Guid.NewGuid(),
        Status = ImportBatchStatus.Pending,
    };

    private static UserImportRow NewRow(int number, string fullName, string email, ImportRowStatus status = ImportRowStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        RowNumber = number,
        FullName = fullName,
        Email = email,
        Role = "Student",
        Status = status,
    };

    private static ApplicationUser NewUser(string fullName, string email) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = true,
    };

    private static FileReadResult ReadSuccess(string content) => new()
    {
        Success = true,
        FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
    };

    private static Mock<GenericRepository<T>> Repository<T>() where T : class
    {
        var set = new Mock<DbSet<T>>();
        var context = new Mock<DbContext>();
        context.Setup(e => e.Set<T>()).Returns(set.Object);
        return new Mock<GenericRepository<T>>(context.Object);
    }
}
