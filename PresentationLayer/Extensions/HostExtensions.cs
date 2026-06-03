using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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



            // var roleMngr = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            // await roleMngr.CreateAsync(new("Admin"));
            // await roleMngr.CreateAsync(new("Student"));
            // await roleMngr.CreateAsync(new("Lecturer"));

            //    var subService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            //    var plans = new List<Plan>
            // {
            //    new()
            //    {
            //        Id = 1,
            //        Name = "Basic",
            //        Tier = 1,
            //        Description = "Perfect for casual learners.",
            //        DailyMessageQuota = 100,
            //        ChatSessionLimit = 10,
            //        DailyFileUploadQuota = 5,
            //        FileLibraryLimit = 20,
            //        AllowAdvancedModels = false,
            //    },

            //    new()
            //    {
            //        Id = 2,
            //        Name = "Advanced",
            //        Tier = 2,
            //        Description = "More conversations and file storage.",
            //        DailyMessageQuota = 500,
            //        ChatSessionLimit = 50,
            //        DailyFileUploadQuota = 20,
            //        FileLibraryLimit = 100,
            //        AllowAdvancedModels = false,
            //    },

            //    new()
            //    {
            //        Id = 3,
            //        Name = "Premium",
            //        Tier = 3,
            //        Description = "Most popular plan for serious students.",
            //        DailyMessageQuota = 2_000,
            //        ChatSessionLimit = 200,
            //        DailyFileUploadQuota = 100,
            //        FileLibraryLimit = 500,
            //        AllowAdvancedModels = true,
            //    },

            //    new()
            //    {
            //        Id = 4,
            //        Name = "Deluxe",
            //        Tier = 4,
            //        Description = "For power users who need higher limits.",
            //        DailyMessageQuota = 10_000,
            //        ChatSessionLimit = 1_000,
            //        DailyFileUploadQuota = 500,
            //        FileLibraryLimit = 2_000,
            //        AllowAdvancedModels = true,
            //    },

            //    new()
            //    {
            //        Id = 5,
            //        Name = "Ultra",
            //        Tier = 5,
            //        Description = "Everything included. No practical limits.",
            //        DailyMessageQuota = AppConstants.UnlimitedQuota,
            //        ChatSessionLimit = AppConstants.UnlimitedQuota,
            //        DailyFileUploadQuota = AppConstants.UnlimitedQuota,
            //        FileLibraryLimit = AppConstants.UnlimitedQuota,
            //        AllowAdvancedModels = true,
            //    }
            // };

            //    var options = new List<PlanOption>
            // {
            //            new()
            //            {
            //                Id = 101,
            //                PlanId = 1,
            //                Name = "Monthly",
            //                DurationDays = 30,
            //                Price = 10_000m
            //            },

            //            new()
            //            {
            //                Id = 201,
            //                PlanId = 2,
            //                Name = "Monthly",
            //                DurationDays = 30,
            //                Price = 20_000m
            //            },

            //            new()
            //            {
            //                Id = 202,
            //                PlanId = 2,
            //                Name = "Quarterly",
            //                DurationDays = 90,
            //                Price = 55_000m
            //            },

            //            new()
            //            {
            //                Id = 301,
            //                PlanId = 3,
            //                Name = "Monthly",
            //                DurationDays = 30,
            //                Price = 30_000m
            //            },

            //            new()
            //            {
            //                Id = 302,
            //                PlanId = 3,
            //                Name = "Semi-Annual",
            //                DurationDays = 180,
            //                Price = 160_000m
            //            },

            //            new()
            //            {
            //                Id = 303,
            //                PlanId= 3,
            //                Name = "Annual",
            //                DurationDays = 365,
            //                Price = 300_000m
            //            },

            //            new()
            //            {
            //                Id = 401,
            //                PlanId =4,
            //                Name = "Quarterly",
            //                DurationDays = 90,
            //                Price = 150_000m
            //            },

            //            new()
            //            {
            //                Id = 402,
            //                PlanId =4,
            //                Name = "Annual",
            //                DurationDays = 365,
            //                Price = 600_000m
            //            },

            //            new()
            //            {
            //                Id = 501,
            //                PlanId =5,
            //                Name = "Annual",
            //                DurationDays = 365,
            //                Price = 1_000_000m
            //            }
            // };

            //    foreach (var plan in plans)
            //    {
            //        await subService.CreatePlanAsync(plan);
            //    }

            //    foreach (var option in options)
            //    {
            //        await subService.CreatePlanOptionAsync(option);
            //    }
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


