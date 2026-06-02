using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Presentation.Extensions;

/// <summary>
/// Extension methods for <see cref="IHost"/> to apply EF Core database migrations on startup.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at application startup.
    /// Handles two scenarios where the database schema was pre-created manually:
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>42P01</c> — The <c>__EFMigrationsHistory</c> table does not exist
    ///     because the database was set up without EF migrations. Falls back to
    ///     <see cref="DatabaseFacade.EnsureCreatedAsync"/> which is a no-op if all tables exist.
    ///   </description></item>
    ///   <item><description>
    ///     <c>42P07</c> — A migration attempts to create a relation that already exists.
    ///     Logs a warning and lets the application continue normally.
    ///   </description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TContext">The EF Core DbContext type to migrate.</typeparam>
    /// <param name="host">The application host instance.</param>
    public static async Task MigrateDb<TContext>(this IHost host)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();
        var context = services.GetRequiredService<TContext>();

        try
        {
            // Check for pending migrations WITHOUT locking the table
            var pending = await context.Database.GetPendingMigrationsAsync();
            var pendingList = pending.ToList();

            if (pendingList.Count == 0)
            {
                logger.LogInformation("Database is already up-to-date. Skipping migration.");
                return;
            }

            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pendingList.Count, string.Join(", ", pendingList));

            // Only lock + migrate when there is actually something to apply
            await context.Database.MigrateAsync();

            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex) when (ex is PostgresException { SqlState: "42P01" or "42P07" } or InvalidOperationException)
        {
            // 42P01 = undefined_table (__EFMigrationsHistory does not exist yet)
            // 42P07 = duplicate_table   (migration tries to create an existing relation)
            // InvalidOperationException = EF Core 9+ pending model changes warning (treated as error)
            logger.LogWarning(
                "EF Core migration skipped or failed (Exception: {Type} - {Message}). " +
                "Falling back to EnsureCreated to check schema consistency.",
                ex.GetType().Name,
                ex.Message);

            await context.Database.EnsureCreatedAsync();
        }
    }
}


