using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Data;

/// <summary>
/// EF Core database context for the EduChatbot application.
/// Uses a custom schema aligned with <c>database-script.sql</c> (not ASP.NET Identity).
/// </summary>
/// <remarks>Initializes a new instance of <see cref="EduChatbotDbContext"/> with options.</remarks>
/// <param name="options">The DbContext configuration options.</param>
public class EduChatbotDbContext(DbContextOptions<EduChatbotDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    // ── Subscription & Payment ───────────────────────────────
    /// <summary>Gets or sets the subscription plans set.</summary>
    public DbSet<Plan> Plans { get; set; }

    /// <summary>Gets or sets the subscription plan options set.</summary>
    public DbSet<PlanOption> PlanOptions { get; set; }

    /// <summary>Gets or sets the subscription orders set.</summary>
    public DbSet<Order> Orders { get; set; }

    /// <summary>Gets or sets the user subscriptions set.</summary>
    public DbSet<Subscription> Subscriptions { get; set; }

    /// <summary>Gets or sets the payment transactions set.</summary>
    public DbSet<Payment> Payments { get; set; }

    // ── Subjects & Documents ─────────────────────────────────
    /// <summary>Gets or sets the subjects set.</summary>
    public DbSet<Subject> Subjects { get; set; }

    /// <summary>Gets or sets the chapters set.</summary>
    public DbSet<Chapter> Chapters { get; set; }

    /// <summary>Gets or sets the documents set.</summary>
    public DbSet<Document> Documents { get; set; }

    /// <summary>Gets or sets the document chunks set.</summary>
    public DbSet<Chunk> Chunks { get; set; }

    // ── Conversations ────────────────────────────────────────
    /// <summary>Gets or sets the conversations set.</summary>
    public DbSet<Conversation> Conversations { get; set; }

    /// <summary>Gets or sets the messages set.</summary>
    public DbSet<Message> Messages { get; set; }

    /// <summary>Gets or sets the citations set.</summary>
    public DbSet<Citation> Citations { get; set; }

    // ── Research & Evaluation ────────────────────────────────
    /// <summary>Gets or sets the test questions set.</summary>
    public DbSet<TestQuestion> TestQuestions { get; set; }

    /// <summary>Gets or sets the experiments set.</summary>
    public DbSet<Experiment> Experiments { get; set; }

    /// <summary>Gets or sets the test responses (experiment results) set.</summary>
    public DbSet<TestResponse> TestResponses { get; set; }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        //optionsBuilder.ConfigureWarnings(w =>
        //    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        modelBuilder
            .Entity<Plan>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<PlanOption>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Order>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Subscription>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Payment>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Subject>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Chapter>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Document>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Chunk>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Conversation>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Citation>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Citation>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<TestQuestion>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<Experiment>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");
        modelBuilder
            .Entity<TestResponse>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");

        modelBuilder.Entity<Plan>()
            .HasIndex(p => p.Tier)
            .IsUnique();

        modelBuilder.Entity<PlanOption>()
            .HasIndex(e => new { e.PlanId, e.DurationDays })
            .IsUnique();
    }
}
