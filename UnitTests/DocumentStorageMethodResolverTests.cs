using Business.Services.Documents.File;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Entities;
using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;

namespace UnitTests;

public class DocumentStorageMethodResolverTests
{
    private Mock<GenericRepository<SubjectStorageConfiguration>> _repository = null!;
    private DocumentStorageMethodResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        var dbContext = new Mock<DbContext>();
        dbContext.Setup(context => context.Set<SubjectStorageConfiguration>())
            .Returns(new Mock<DbSet<SubjectStorageConfiguration>>().Object);
        _repository = new Mock<GenericRepository<SubjectStorageConfiguration>>(dbContext.Object);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SubjectStorageConfigurations).Returns(_repository.Object);
        _resolver = new DocumentStorageMethodResolver(unitOfWork.Object);
    }

    [Test]
    public async Task ResolveForPersistenceAsync_UsesSubjectConfiguration()
    {
        SetConfigurations(new SubjectStorageConfiguration
        {
            Id = 42,
            StorageMethod = FileStorageMethod.LocalHardDrive,
        });

        var result = await _resolver.ResolveForPersistenceAsync(new Document { SubjectId = 42 });

        Assert.That(result, Is.EqualTo(FileStorageMethod.LocalHardDrive));
    }

    [TestCase(null)]
    [TestCase(FileStorageMethod.Unspecified)]
    public async Task ResolveForPersistenceAsync_DefaultsToSupabase_WhenPolicyDoesNotSelectStorage(
        FileStorageMethod? configuredMethod)
    {
        SetConfigurations(configuredMethod.HasValue
            ? new SubjectStorageConfiguration { Id = 42, StorageMethod = configuredMethod }
            : null);

        var result = await _resolver.ResolveForPersistenceAsync(new Document { SubjectId = 42 });

        Assert.That(result, Is.EqualTo(FileStorageMethod.Supabase));
    }

    private void SetConfigurations(SubjectStorageConfiguration? configuration)
    {
        var configurations = configuration == null
            ? Array.Empty<SubjectStorageConfiguration>()
            : [configuration];

        _repository.Setup(repository => repository.GetAsync(
                It.IsAny<string[]>(),
                It.IsAny<Expression<Func<SubjectStorageConfiguration, bool>>>(),
                It.IsAny<Func<IQueryable<SubjectStorageConfiguration>, IOrderedQueryable<SubjectStorageConfiguration>>>(),
                It.IsAny<(int, int)>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(configurations);
    }
}
