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
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    /// <summary>Gets or sets the subscription plan options set.</summary>
    public DbSet<SubscriptionPlanOption> SubscriptionPlanOptions { get; set; }

    /// <summary>Gets or sets the subscription purchases set.</summary>
    public DbSet<SubscriptionPurchase> SubscriptionPurchases { get; set; }

    /// <summary>Gets or sets the user subscriptions set.</summary>
    public DbSet<UserSubscription> UserSubscriptions { get; set; }

    /// <summary>Gets or sets the payment transactions set.</summary>
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

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
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
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

        modelBuilder.Entity<SubscriptionPlan>()
            .HasIndex(p => p.Tier)
            .IsUnique();

        // modelBuilder.Entity<DocumentChunk>(entity =>
        // {
        //     entity.ToTable("DocumentChunks");
        //     entity.HasKey(e => e.Id);
        //     entity.Property(e => e.EmbeddingVector)
        //           .HasColumnType("vector(1536)");
        // });
    }
}
