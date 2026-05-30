using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data;

/// <summary>
/// EF Core database context for the EduChatbot application.
/// Uses a custom schema aligned with <c>database-script.sql</c> (not ASP.NET Identity).
/// </summary>
public class EduChatbotDbContext : DbContext
{
    // ── Authentication ───────────────────────────────────────
    /// <summary>Gets or sets the users set.</summary>
    public DbSet<ApplicationUser> Users { get; set; }

    /// <summary>Gets or sets the roles set.</summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>Gets or sets the user-role join set.</summary>
    public DbSet<UserRole> UserRoles { get; set; }

    // ── Subscription & Payment ───────────────────────────────
    /// <summary>Gets or sets the subscription plans set.</summary>
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

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

    /// <summary>Initializes a new instance of <see cref="EduChatbotDbContext"/> with options.</summary>
    /// <param name="options">The DbContext configuration options.</param>
    public EduChatbotDbContext(DbContextOptions<EduChatbotDbContext> options) : base(options) { }

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

        // ── users ────────────────────────────────────────────
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── roles ────────────────────────────────────────────
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(r => r.Id);
        });

        // ── user_roles (composite key) ───────────────────────
        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(ur => new { ur.UserId, ur.RoleId });

            e.HasOne(ur => ur.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(ur => ur.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ur => ur.Role)
             .WithMany(r => r.UserRoles)
             .HasForeignKey(ur => ur.RoleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── subscription_plans ───────────────────────────────
        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.ToTable("subscription_plans");
            e.HasKey(p => p.Id);
            e.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── user_subscriptions ───────────────────────────────
        modelBuilder.Entity<UserSubscription>(e =>
        {
            e.ToTable("user_subscriptions");
            e.HasKey(us => us.Id);
            e.Property(us => us.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(us => us.Status).HasConversion<string>();

            e.HasOne(us => us.User)
             .WithMany()
             .HasForeignKey(us => us.UserId);

            e.HasOne(us => us.Plan)
             .WithMany(p => p.UserSubscriptions)
             .HasForeignKey(us => us.PlanId);
        });

        // ── payment_transactions ─────────────────────────────
        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.ToTable("payment_transactions");
            e.HasKey(pt => pt.Id);
            e.Property(pt => pt.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(pt => pt.PaymentStatus).HasConversion<string>();

            e.HasOne(pt => pt.Subscription)
             .WithMany(us => us.PaymentTransactions)
             .HasForeignKey(pt => pt.SubscriptionId);
        });

        // ── subjects ─────────────────────────────────────────
        modelBuilder.Entity<Subject>(e =>
        {
            e.ToTable("subjects");
            e.HasKey(s => s.Id);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── chapters ─────────────────────────────────────────
        modelBuilder.Entity<Chapter>(e =>
        {
            e.ToTable("chapters");
            e.HasKey(c => c.Id);
            e.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

            e.HasOne(c => c.Subject)
             .WithMany(s => s.Chapters)
             .HasForeignKey(c => c.SubjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── documents ────────────────────────────────────────
        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(d => d.UploadedAt).HasDefaultValueSql("NOW()");

            e.HasOne(d => d.Subject)
             .WithMany()
             .HasForeignKey(d => d.SubjectId);

            e.HasOne(d => d.Chapter)
             .WithMany(c => c.Documents)
             .HasForeignKey(d => d.ChapterId);

            e.HasOne(d => d.Uploader)
             .WithMany()
             .HasForeignKey(d => d.UploadedBy);
        });

        // ── document_chunks ──────────────────────────────────
        modelBuilder.Entity<Chunk>(e =>
        {
            e.ToTable("document_chunks");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

            e.HasOne(c => c.Document)
             .WithMany(d => d.Chunks)
             .HasForeignKey(c => c.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── conversations ────────────────────────────────────
        modelBuilder.Entity<Conversation>(e =>
        {
            e.ToTable("conversations");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

            e.HasOne(c => c.User)
             .WithMany(u => u.Conversations)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── messages ─────────────────────────────────────────
        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(m => m.SentAt).HasDefaultValueSql("NOW()");
            e.Property(m => m.SenderRole).HasConversion<string>();

            e.HasOne(m => m.Conversation)
             .WithMany(c => c.Messages)
             .HasForeignKey(m => m.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── citations ────────────────────────────────────────
        modelBuilder.Entity<Citation>(e =>
        {
            e.ToTable("citations");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

            e.HasOne(c => c.Message)
             .WithMany(m => m.Citations)
             .HasForeignKey(c => c.MessageId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Document)
             .WithMany()
             .HasForeignKey(c => c.DocumentId);

            e.HasOne(c => c.Chunk)
             .WithMany(ch => ch.Citations)
             .HasForeignKey(c => c.ChunkId);
        });

        // ── test_questions ───────────────────────────────────
        modelBuilder.Entity<TestQuestion>(e =>
        {
            e.ToTable("test_questions");
            e.HasKey(q => q.Id);
            e.Property(q => q.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(q => q.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── experiments ──────────────────────────────────────
        modelBuilder.Entity<Experiment>(e =>
        {
            e.ToTable("experiments");
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(ex => ex.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── experiment_results ────────────────────────────────
        modelBuilder.Entity<TestResponse>(e =>
        {
            e.ToTable("experiment_results");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("uuid_generate_v4()");
            e.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");

            e.HasOne(r => r.Experiment)
             .WithMany(ex => ex.TestResponses)
             .HasForeignKey(r => r.ExperimentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.TestQuestion)
             .WithMany(q => q.TestResponses)
             .HasForeignKey(r => r.TestQuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
