using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Extensions;

public static class HostExtensions
{
    public static async Task MigrateDb<TContext>(this IHost host)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TContext>().Database.MigrateAsync();
    }
}
