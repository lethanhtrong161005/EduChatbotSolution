using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Domain.Entities;
using Domain.Common;
using Domain.Contracts;
using Npgsql;
using Org.BouncyCastle.Tls;

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

            if (pendingList.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                    pendingList.Count, string.Join(", ", pendingList));
                await context.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("Database is already up-to-date. Skipping migration.");
            }
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

    public static async Task SeedDbAsync<TContext>(this IHost host)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var logger = services.GetRequiredService<ILogger<TContext>>();

        if ((await roleManager.FindByNameAsync("Admin")) != null)
        {
            logger.LogInformation("Roles already exist. Skipping role seed.");
            goto USER;
        }
        await roleManager.CreateAsync(new("Admin"));
        await roleManager.CreateAsync(new("Student"));
        await roleManager.CreateAsync(new("Lecturer"));

    USER:
        // Ensure default admin user exists for development testing
        var adminEmail = "admin@educhatai.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123456!"),
            };
            var createResult = await userManager.CreateAsync(adminUser);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                await userManager.AddClaimsAsync(adminUser,
                [
                    new(System.Security.Claims.ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                        new(System.Security.Claims.ClaimTypes.Email, adminUser.Email ?? adminEmail),
                        new(System.Security.Claims.ClaimTypes.Name, adminUser.FullName),
                        new(System.Security.Claims.ClaimTypes.Role, "Admin"),
                    ]);
                logger.LogInformation("Successfully seeded default Admin user.");
            }
            else
            {
                logger.LogError("Failed to seed default Admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

    SUBSCRIPTION:
        if ((await unitOfWork.Plans.GetAsync()).Any())
        {
            logger.LogInformation("Plans already exist. Skipping subscription seed.");
            goto DOCUMENT;
        }

        var plans = new List<Plan>
         {
            new()
            {
                Id = 1,
                Name = "Basic",
                Tier = 1,
                Description = "Perfect for casual learners.",
                DailyMessageQuota = 100,
                ChatSessionLimit = 10,
                DailyFileUploadQuota = 5,
                FileLibraryLimit = 20,
                AllowAdvancedModels = false,
            },

            new()
            {
                Id = 2,
                Name = "Advanced",
                Tier = 2,
                Description = "More conversations and file storage.",
                DailyMessageQuota = 500,
                ChatSessionLimit = 50,
                DailyFileUploadQuota = 20,
                FileLibraryLimit = 100,
                AllowAdvancedModels = false,
            },

            new()
            {
                Id = 3,
                Name = "Premium",
                Tier = 3,
                Description = "Most popular plan for serious students.",
                DailyMessageQuota = 2_000,
                ChatSessionLimit = 200,
                DailyFileUploadQuota = 100,
                FileLibraryLimit = 500,
                AllowAdvancedModels = true,
            },

            new()
            {
                Id = 4,
                Name = "Deluxe",
                Tier = 4,
                Description = "For power users who need higher limits.",
                DailyMessageQuota = 10_000,
                ChatSessionLimit = 1_000,
                DailyFileUploadQuota = 500,
                FileLibraryLimit = 2_000,
                AllowAdvancedModels = true,
            },

            new()
            {
                Id = 5,
                Name = "Ultra",
                Tier = 5,
                Description = "Everything included. No practical limits.",
                DailyMessageQuota = AppConstants.UnlimitedQuota,
                ChatSessionLimit = AppConstants.UnlimitedQuota,
                DailyFileUploadQuota = AppConstants.UnlimitedQuota,
                FileLibraryLimit = AppConstants.UnlimitedQuota,
                AllowAdvancedModels = true,
            }
         };

        var options = new List<PlanOption>
         {
                    new()
                    {
                        Id = 101,
                        PlanId = 1,
                        Name = "Monthly",
                        DurationDays = 30,
                        Price = 10_000m
                    },

                    new()
                    {
                        Id = 201,
                        PlanId = 2,
                        Name = "Monthly",
                        DurationDays = 30,
                        Price = 20_000m
                    },

                    new()
                    {
                        Id = 202,
                        PlanId = 2,
                        Name = "Quarterly",
                        DurationDays = 90,
                        Price = 55_000m
                    },

                    new()
                    {
                        Id = 301,
                        PlanId = 3,
                        Name = "Monthly",
                        DurationDays = 30,
                        Price = 30_000m
                    },

                    new()
                    {
                        Id = 302,
                        PlanId = 3,
                        Name = "Semi-Annual",
                        DurationDays = 180,
                        Price = 160_000m
                    },

                    new()
                    {
                        Id = 303,
                        PlanId= 3,
                        Name = "Annual",
                        DurationDays = 365,
                        Price = 300_000m
                    },

                    new()
                    {
                        Id = 401,
                        PlanId =4,
                        Name = "Quarterly",
                        DurationDays = 90,
                        Price = 150_000m
                    },

                    new()
                    {
                        Id = 402,
                        PlanId =4,
                        Name = "Annual",
                        DurationDays = 365,
                        Price = 600_000m
                    },

                    new()
                    {
                        Id = 501,
                        PlanId =5,
                        Name = "Annual",
                        DurationDays = 365,
                        Price = 1_000_000m
                    }
         };

        foreach (var plan in plans)
        {
            unitOfWork.Plans.Insert(plan);
        }

        foreach (var option in options)
        {
            unitOfWork.PlanOptions.Insert(option);
        }

        await unitOfWork.SaveAsync();

    DOCUMENT:
        var uploader = await userManager.Users.FirstOrDefaultAsync();

        if (uploader == null)
        {
            logger.LogWarning("Skipping document seed: no users exist.");
            return;
        }

        if ((await unitOfWork.Subjects.GetAsync()).Any())
        {
            logger.LogInformation("Subjects already exist. Skipping seed.");
            return;
        }

        var rnd = new Random();

        /* ============ SUBJECT ============ */
        var architecture = new Subject
        {
            Code = "SE401",
            Name = "Software Architecture",
            Description = "Advanced software architecture patterns and principles."
        };

        var ai = new Subject
        {
            Code = "AI301",
            Name = "Artificial Intelligence",
            Description = "Foundations of modern AI."
        };

        var database = new Subject
        {
            Code = "DB201",
            Name = "Database Systems",
            Description = "Relational and non-relational databases."
        };

        unitOfWork.Subjects.Insert(architecture);
        unitOfWork.Subjects.Insert(ai);
        unitOfWork.Subjects.Insert(database);

        await unitOfWork.SaveAsync();

        /* ============ CHAPTER ============ */
        var chapters = new List<Chapter>
{
    // SE401

    new() { SubjectId = architecture.Id, Name = "Introduction to Architecture", ChapterNumber = 1 },
    new() { SubjectId = architecture.Id, Name = "Architectural Styles", ChapterNumber = 2 },
    new() { SubjectId = architecture.Id, Name = "Layered Architecture", ChapterNumber = 3 },
    new() { SubjectId = architecture.Id, Name = "Microservices", ChapterNumber = 4 },
    new() { SubjectId = architecture.Id, Name = "Event-Driven Systems", ChapterNumber = 5 },
    new() { SubjectId = architecture.Id, Name = "Domain Driven Design", ChapterNumber = 6 },
    new() { SubjectId = architecture.Id, Name = "Quality Attributes", ChapterNumber = 7 },
    new() { SubjectId = architecture.Id, Name = "Architecture Evaluation", ChapterNumber = 8 },

    // AI301

    new() { SubjectId = ai.Id, Name = "AI Fundamentals", ChapterNumber = 1 },
    new() { SubjectId = ai.Id, Name = "Search Algorithms", ChapterNumber = 2 },
    new() { SubjectId = ai.Id, Name = "Knowledge Representation", ChapterNumber = 3 },
    new() { SubjectId = ai.Id, Name = "Machine Learning Basics", ChapterNumber = 4 },
    new() { SubjectId = ai.Id, Name = "Neural Networks", ChapterNumber = 5 },
    new() { SubjectId = ai.Id, Name = "Natural Language Processing", ChapterNumber = 6 },
    new() { SubjectId = ai.Id, Name = "Ethics in AI", ChapterNumber = 7 },

    // DB201

    new() { SubjectId = database.Id, Name = "Relational Model", ChapterNumber = 1 },
    new() { SubjectId = database.Id, Name = "SQL Fundamentals", ChapterNumber = 2 },
    new() { SubjectId = database.Id, Name = "Normalization", ChapterNumber = 3 },
    new() { SubjectId = database.Id, Name = "Transactions", ChapterNumber = 4 },
    new() { SubjectId = database.Id, Name = "Indexing", ChapterNumber = 5 },
    new() { SubjectId = database.Id, Name = "Query Optimization", ChapterNumber = 6 },
    new() { SubjectId = database.Id, Name = "Distributed Databases", ChapterNumber = 7 },
    new() { SubjectId = database.Id, Name = "NoSQL Databases", ChapterNumber = 8 },
    new() { SubjectId = database.Id, Name = "Vector Databases", ChapterNumber = 9 }
};

        foreach (var chapter in chapters)
        {
            unitOfWork.Chapters.Insert(chapter);
        }

        await unitOfWork.SaveAsync();

        /* ============ DOCUMENT ============ */

        var introArch = chapters.Single(x => x.Name == "Introduction to Architecture");
        var styles = chapters.Single(x => x.Name == "Architectural Styles");
        var layered = chapters.Single(x => x.Name == "Layered Architecture");
        var microservices = chapters.Single(x => x.Name == "Microservices");
        var eventDriven = chapters.Single(x => x.Name == "Event-Driven Systems");
        var ddd = chapters.Single(x => x.Name == "Domain Driven Design");
        var quality = chapters.Single(x => x.Name == "Quality Attributes");
        var evaluation = chapters.Single(x => x.Name == "Architecture Evaluation");

        var aiFundamentals = chapters.Single(x => x.Name == "AI Fundamentals");
        var searchAlgorithms = chapters.Single(x => x.Name == "Search Algorithms");
        var knowledgeRepresentation = chapters.Single(x => x.Name == "Knowledge Representation");
        var machineLearning = chapters.Single(x => x.Name == "Machine Learning Basics");
        var neuralNetworks = chapters.Single(x => x.Name == "Neural Networks");
        var nlp = chapters.Single(x => x.Name == "Natural Language Processing");
        var aiEthics = chapters.Single(x => x.Name == "Ethics in AI");

        var relationalModel = chapters.Single(x => x.Name == "Relational Model");
        var sqlFundamentals = chapters.Single(x => x.Name == "SQL Fundamentals");
        var normalization = chapters.Single(x => x.Name == "Normalization");
        var transactions = chapters.Single(x => x.Name == "Transactions");
        var indexing = chapters.Single(x => x.Name == "Indexing");
        var queryOptimization = chapters.Single(x => x.Name == "Query Optimization");
        var distributedDatabases = chapters.Single(x => x.Name == "Distributed Databases");
        var noSql = chapters.Single(x => x.Name == "NoSQL Databases");
        var vectorDatabases = chapters.Single(x => x.Name == "Vector Databases");

        /* =========================================================
         * SOFTWARE ARCHITECTURE
         * ========================================================= */

        AddDoc(introArch, "Architecture Overview", "architecture-overview.pdf");
        AddDoc(introArch, "Course Syllabus", "course-syllabus.docx");
        AddDoc(introArch, "History of Software Architecture", "architecture-history.pptx");

        AddDoc(styles, "MVC Pattern", "mvc-pattern.pdf");
        AddDoc(styles, "Client Server Architecture", "client-server.pdf");
        AddDoc(styles, "Pipe and Filter Pattern", "pipe-filter.pdf");
        AddDoc(styles, "Architectural Styles Comparison", "styles-comparison.docx");

        AddDoc(layered, "Layered Architecture Notes", "layered-notes.pdf");
        AddDoc(layered, "N-Tier Systems", "n-tier-systems.docx");

        AddDoc(microservices, "Introduction to Microservices", "microservices-intro.pdf");
        AddDoc(microservices, "Service Discovery", "service-discovery.pdf");
        AddDoc(microservices, "API Gateway Pattern", "api-gateway.pptx");
        AddDoc(microservices, "Saga Pattern", "saga-pattern.pdf");

        /* Event Driven Systems intentionally empty */

        AddDoc(ddd, "Bounded Contexts", "bounded-contexts.pdf");
        AddDoc(ddd, "Aggregates and Repositories", "aggregates.pdf");
        AddDoc(ddd, "Domain Events", "domain-events.docx");

        AddDoc(quality, "Scalability Fundamentals", "scalability.pdf");
        AddDoc(quality, "Maintainability Metrics", "maintainability.pdf");

        AddDoc(evaluation, "ATAM Methodology", "atam.pdf");
        AddDoc(evaluation, "Architecture Review Checklist", "review-checklist.docx");


        /* =========================================================
         * ARTIFICIAL INTELLIGENCE
         * ========================================================= */

        AddDoc(aiFundamentals, "What is Artificial Intelligence", "intro-ai.pdf");
        AddDoc(aiFundamentals, "History of AI", "history-ai.docx");
        AddDoc(aiFundamentals, "AI Applications", "ai-applications.pdf");
        AddDoc(aiFundamentals, "Intelligent Agents", "intelligent-agents.pptx");

        AddDoc(searchAlgorithms, "Breadth First Search", "bfs.pdf");
        AddDoc(searchAlgorithms, "Depth First Search", "dfs.pdf");
        AddDoc(searchAlgorithms, "A Star Search", "astar-search.pdf");

        /* Knowledge Representation intentionally empty */

        AddDoc(machineLearning, "Machine Learning Overview", "ml-overview.pdf");
        AddDoc(machineLearning, "Supervised Learning", "supervised-learning.pdf");
        AddDoc(machineLearning, "Unsupervised Learning", "unsupervised-learning.pdf");
        AddDoc(machineLearning, "Feature Engineering", "feature-engineering.docx");
        AddDoc(machineLearning, "Model Evaluation", "model-evaluation.pdf");

        AddDoc(neuralNetworks, "Perceptrons", "perceptrons.pdf");
        AddDoc(neuralNetworks, "Backpropagation", "backpropagation.pdf");
        AddDoc(neuralNetworks, "Activation Functions", "activation-functions.pdf");
        AddDoc(neuralNetworks, "Deep Learning Basics", "deep-learning.pptx");

        AddDoc(nlp, "Natural Language Processing Overview", "nlp-overview.pdf");
        AddDoc(nlp, "Text Classification", "text-classification.docx");

        AddDoc(aiEthics, "AI Ethics Principles", "ai-ethics.pdf");
        AddDoc(aiEthics, "Bias and Fairness", "bias-fairness.pdf");


        /* =========================================================
         * DATABASE SYSTEMS
         * ========================================================= */

        AddDoc(relationalModel, "Relational Model Fundamentals", "relational-model.pdf");
        AddDoc(relationalModel, "Entities and Relationships", "er-model.docx");
        AddDoc(relationalModel, "Relational Algebra", "relational-algebra.pdf");

        AddDoc(sqlFundamentals, "SQL Basics", "sql-basics.pdf");
        AddDoc(sqlFundamentals, "SELECT Queries", "select-queries.pdf");
        AddDoc(sqlFundamentals, "JOIN Operations", "joins.pdf");
        AddDoc(sqlFundamentals, "Grouping and Aggregation", "grouping.pdf");
        AddDoc(sqlFundamentals, "Stored Procedures", "stored-procedures.docx");

        AddDoc(normalization, "First Normal Form", "1nf.pdf");
        AddDoc(normalization, "Second and Third Normal Form", "2nf-3nf.pdf");
        AddDoc(normalization, "Boyce Codd Normal Form", "bcnf.pdf");

        /* Transactions intentionally empty */

        AddDoc(indexing, "Database Indexes", "indexes.pdf");
        AddDoc(indexing, "B Tree Structures", "btree.pdf");

        AddDoc(queryOptimization, "Execution Plans", "execution-plans.pdf");
        AddDoc(queryOptimization, "Cost Based Optimization", "cost-optimization.pdf");
        AddDoc(queryOptimization, "Query Tuning", "query-tuning.docx");

        AddDoc(distributedDatabases, "Distributed Database Concepts", "distributed-db.pdf");
        AddDoc(distributedDatabases, "Replication Strategies", "replication.pdf");

        AddDoc(noSql, "Introduction to NoSQL", "nosql-intro.pdf");
        AddDoc(noSql, "Document Databases", "document-databases.pdf");

        AddDoc(vectorDatabases, "Vector Database Fundamentals", "vector-databases.pdf");
        AddDoc(vectorDatabases, "Semantic Search Systems", "semantic-search.pdf");

        void AddDoc(Chapter chapter, string title, string fileName)
        {
            unitOfWork.Documents.Insert(new Document
            {
                ChapterId = chapter.Id,
                UploaderId = uploader.Id,

                Title = title,
                Description = $"{title} learning material.",

                OriginalFileName = fileName,
                FileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}",

                FileType = RandomFileType(rnd),
                Status = RandomStatus(rnd),

                FileSize = rnd.Next(250 * 1024, 15 * 1024 * 1024),

                FilePath = Path.Combine(
                    Path.GetTempPath(),
                    "EduChatAI",
                    $"{Guid.NewGuid()}{Path.GetExtension(fileName)}"),

                UploadedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 180)),
            });
        }

        await unitOfWork.SaveAsync();

        unitOfWork.SubjectMemberships.Insert(
            new SubjectMembership
            {
                UserId = uploader.Id,
                SubjectId = architecture.Id,
                Role = MembershipRole.Chief,
                AssignedAt = DateTime.UtcNow
            });

        unitOfWork.SubjectMemberships.Insert(
            new SubjectMembership
            {
                UserId = uploader.Id,
                SubjectId = ai.Id,
                Role = MembershipRole.Lecturer,
                AssignedAt = DateTime.UtcNow
            });

        await unitOfWork.SaveAsync();
    }

    static DocumentType RandomFileType(Random rnd)
    {
        return rnd.Next(5) switch
        {
            0 => DocumentType.PDF,
            1 => DocumentType.DOCX,
            2 => DocumentType.PPTX,
            3 => DocumentType.TXT,
            _ => DocumentType.HTML
        };
    }

    static DocumentStatus RandomStatus(Random rnd)
    {
        return rnd.Next(100) switch
        {
            < 75 => DocumentStatus.Indexed,
            < 85 => DocumentStatus.Embedding,
            < 92 => DocumentStatus.Chunking,
            < 98 => DocumentStatus.Parsing,
            _ => DocumentStatus.Failed
        };
    }
}
