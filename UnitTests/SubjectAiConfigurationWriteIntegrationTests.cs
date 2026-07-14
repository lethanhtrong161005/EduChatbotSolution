using DataAccess.Data;
using DataAccess.Repositories;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

[TestFixture, NonParallelizable]
public sealed class SubjectAiConfigurationWriteIntegrationTests
{
    private const string ConnectionVariable = "EDUCHATAI_PHASE2_TEST_DATABASE";
    private readonly List<int> _subjectIds = [];
    private string _connectionString = null!;

    [SetUp]
    public void SetUp()
    {
        _connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? "";
        if (string.IsNullOrWhiteSpace(_connectionString)) Assert.Ignore($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
    }

    [TearDown]
    public async Task TearDown()
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || _subjectIds.Count == 0) return;
        await using var context = CreateContext();
        await context.SubjectAiConfigurations.Where(e => _subjectIds.Contains(e.Id)).ExecuteDeleteAsync();
        await context.Subjects.Where(e => _subjectIds.Contains(e.Id)).ExecuteDeleteAsync();
    }

    [Test]
    public async Task SaveConfiguration_ReadySubject_ReplacesOverrides()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Ready, 400);
        await using var context = CreateContext();

        var saved = await new SubjectIndexRepository(context).SaveConfigurationAsync(new SubjectAiConfiguration
        {
            Id = subjectId,
            ChunkSize = 600,
            ChatPrompt = "Replacement prompt",
        });

        await using var verificationContext = CreateContext();
        var stored = await verificationContext.SubjectAiConfigurations.AsNoTracking().SingleAsync(e => e.Id == subjectId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(saved.Id, Is.EqualTo(subjectId));
            Assert.That(stored.ChunkSize, Is.EqualTo(600));
            Assert.That(stored.ChatPrompt, Is.EqualTo("Replacement prompt"));
        }
    }

    [Test]
    public async Task SaveConfiguration_ReindexingSubject_ThrowsConflictWithoutChangingConfiguration()
    {
        var subjectId = await CreateSubjectAsync(SubjectIndexAvailability.Reindexing, 400);
        await using var context = CreateContext();

        var exception = Assert.ThrowsAsync<EntityConflictException>(() =>
            new SubjectIndexRepository(context).SaveConfigurationAsync(new SubjectAiConfiguration { Id = subjectId, ChunkSize = 600 }));

        await using var verificationContext = CreateContext();
        var stored = await verificationContext.SubjectAiConfigurations.AsNoTracking().SingleAsync(e => e.Id == subjectId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Property, Is.EqualTo(nameof(Subject.IndexAvailability)));
            Assert.That(stored.ChunkSize, Is.EqualTo(400));
        }
    }

    private async Task<int> CreateSubjectAsync(SubjectIndexAvailability availability, int chunkSize)
    {
        await using var context = CreateContext();
        var subject = new Subject { Code = $"CFG-{Guid.NewGuid():N}", Name = "Configuration write test", IndexAvailability = availability };
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        context.SubjectAiConfigurations.Add(new SubjectAiConfiguration { Id = subject.Id, ChunkSize = chunkSize });
        await context.SaveChangesAsync();

        _subjectIds.Add(subject.Id);
        return subject.Id;
    }

    private EduChatAiDbContext CreateContext() => new(new DbContextOptionsBuilder<EduChatAiDbContext>()
        .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
        .UseSnakeCaseNamingConvention()
        .Options);
}
