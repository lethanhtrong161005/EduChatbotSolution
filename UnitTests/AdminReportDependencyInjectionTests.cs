using Business.Services.Reports;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Extensions;

namespace UnitTests;

[TestFixture]
public sealed class AdminReportDependencyInjectionTests
{
    [Test]
    public void AddAdminReports_ResolvesExactlyOneRealServiceAndSystemClock()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IUnitOfWork>().Object);

        services.AddAdminReports();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var reports = scope.ServiceProvider.GetServices<IAdminReportService>().ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(reports, Has.Length.EqualTo(1));
            Assert.That(reports[0], Is.TypeOf<AdminReportService>());
            Assert.That(scope.ServiceProvider.GetRequiredService<TimeProvider>(), Is.SameAs(TimeProvider.System));
        }
    }
}
