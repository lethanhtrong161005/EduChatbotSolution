using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Presentation.Extensions;

namespace UnitTests;

[TestFixture]
public sealed class AdminReportDemoDataHostExtensionTests
{
    [TestCase("Development", "true", true)]
    [TestCase("Development", "false", false)]
    [TestCase("Production", "true", false)]
    public void ShouldSeed_RequiresBothDevelopmentAndExplicitOptIn(string environmentName, string setting, bool expected)
    {
        var environment = new TestHostEnvironment { EnvironmentName = environmentName };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoData:AdminReports:Enabled"] = setting,
            })
            .Build();

        Assert.That(AdminReportDemoDataHostExtensions.ShouldSeed(environment, configuration), Is.EqualTo(expected));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "UnitTests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
