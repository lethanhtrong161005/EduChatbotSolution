using DataAccess.Seeding;

namespace Presentation.Extensions;

public static class AdminReportDemoDataHostExtensions
{
    public static async Task SeedAdminReportDemoDataAsync(
        this IHost host,
        CancellationToken cxlTkn = default)
    {
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        if (!ShouldSeed(environment, configuration)) return;

        using var scope = host.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<AdminReportDemoDataSeeder>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AdminReportDemoDataSeeder>>();
        await seeder.SeedAsync(cxlTkn);
        logger.LogInformation("Admin report development demo data refreshed.");
    }

    internal static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment()
        && configuration.GetValue("DemoData:AdminReports:Enabled", false);
}
