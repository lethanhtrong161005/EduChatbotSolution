using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

[TestFixture, NonParallelizable]
public sealed class SubjectIndexRepositoryIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private readonly List<int> _subjectIds = [];
    private string _connectionString = null!;

    [SetUp]
    public void SetUp()
    {
        _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString)) Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
    }

    [TearDown]
    public async Task TearDown()
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _subjectIds.Count == 0) return;
        await using var context = CreateContext();
        await context.Subjects.Where(e => _subjectIds.Contains(e.Id)).ExecuteDeleteAsync();
    }

    [Test]
    public async Task AcquireExclusiveReindex_ReadySubject_ReturnsPreviousAndSetsReindexing()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Ready);
        await using var context = CreateContext();

        var previous = await new SubjectIndexRepository(context).AcquireExclusiveReindexAsync(subjectId);
        var current = await context.Subjects.AsNoTracking().Where(e => e.Id == subjectId).Select(e => e.IndexAvailability).SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previous, Is.EqualTo(SubjectIndexAvailability.Ready));
            Assert.That(current, Is.EqualTo(SubjectIndexAvailability.Reindexing));
        }
    }

    [Test]
    public async Task AcquireExclusiveReindex_FailedSubject_ReturnsFailedAndSetsReindexing()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Failed);
        await using var context = CreateContext();

        var previous = await new SubjectIndexRepository(context).AcquireExclusiveReindexAsync(subjectId);
        var current = await context.Subjects.AsNoTracking().Where(e => e.Id == subjectId).Select(e => e.IndexAvailability).SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previous, Is.EqualTo(SubjectIndexAvailability.Failed));
            Assert.That(current, Is.EqualTo(SubjectIndexAvailability.Reindexing));
        }
    }

    [Test]
    public async Task AcquireExclusiveReindex_ConcurrentCalls_OnlyOneAcquires()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Ready);
        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();

        var outcomes = await Task.WhenAll(
            AttemptAcquireAsync(new SubjectIndexRepository(firstContext), subjectId),
            AttemptAcquireAsync(new SubjectIndexRepository(secondContext), subjectId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcomes.Count(e => e == null), Is.EqualTo(1));
            Assert.That(outcomes.Count(e => e is EntityConflictException), Is.EqualTo(1));
        }
    }

    [Test]
    public void AcquireExclusiveReindex_MissingSubject_ThrowsNotFound()
    {
        using var context = CreateContext();

        Assert.ThrowsAsync<EntityNotFoundException>(() => new SubjectIndexRepository(context).AcquireExclusiveReindexAsync(int.MaxValue));
    }

    [Test]
    public async Task SetAvailability_UpdatesSubject()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Reindexing);
        await using var context = CreateContext();

        await new SubjectIndexRepository(context).SetAvailabilityAsync(subjectId, SubjectIndexAvailability.Ready);
        var current = await context.Subjects.AsNoTracking().Where(e => e.Id == subjectId).Select(e => e.IndexAvailability).SingleAsync();

        Assert.That(current, Is.EqualTo(SubjectIndexAvailability.Ready));
    }

    private async Task<int> CreateSubjectAsync(SubjectIndexAvailability availability)
    {
        await using var context = CreateContext();
        var subject = new Subject { Code = $"IDX-{Guid.NewGuid():N}", Name = "Index repository test", IndexAvailability = availability };
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();
        _subjectIds.Add(subject.Id);
        return subject.Id;
    }

    private static async Task<Exception?> AttemptAcquireAsync(SubjectIndexRepository repository, int subjectId)
    {
        try
        {
            await repository.AcquireExclusiveReindexAsync(subjectId);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private EduChatAiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EduChatAiDbContext>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new EduChatAiDbContext(options);
    }
}
