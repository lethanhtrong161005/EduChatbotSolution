using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Data;

/// <summary>
/// EF Core database context for the EduChatAI application.
/// Uses a custom schema aligned with <c>database-script.sql</c> (not ASP.NET Identity).
/// </summary>
/// <remarks>Initializes a new instance of <see cref="EduChatAiDbContext"/> with options.</remarks>
/// <param name="options">The DbContext configuration options.</param>
public class EduChatAiDbContext(DbContextOptions<EduChatAiDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, IdentityUserClaim<Guid>, ApplicationUserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
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

    public DbSet<Membership> Memberships { get; set; }

    public DbSet<SubjectStorageConfiguration> SubjectStorageConfigurations { get; set; }

    public DbSet<SubjectAiConfiguration> SubjectAiConfigurations { get; set; }

    public DbSet<GlobalAiConfiguration> GlobalAiConfigurations { get; set; }

    /// <summary>Gets or sets the chapters set.</summary>
    public DbSet<Chapter> Chapters { get; set; }

    /// <summary>Gets or sets the documents set.</summary>
    public DbSet<Document> Documents { get; set; }

    public DbSet<DocumentChapter> DocumentChapters { get; set; }

    public DbSet<DocumentComment> DocumentComments { get; set; }

    public DbSet<ParsedSection> ParsedSections { get; set; }

    /// <summary>Gets or sets the document chunks set.</summary>
    public DbSet<Chunk> Chunks { get; set; }

    // ── Conversations ────────────────────────────────────────
    /// <summary>Gets or sets the conversations set.</summary>
    public DbSet<ChatSession> ChatSessions { get; set; }

    public DbSet<ChatSessionTitleGenerationSettings> ChatSessionTitleGenerationSettings { get; set; }

    public DbSet<ChatSessionTitleGenerationMetrics> ChatSessionTitleGenerationMetrics { get; set; }

    /// <summary>Gets or sets the messages set.</summary>
    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<ChatMessageGenerationSettings> ChatMessageGenerationSettings { get; set; }

    public DbSet<ChatMessageGenerationMetrics> ChatMessageGenerationMetrics { get; set; }

    /// <summary>Gets or sets the citations set.</summary>
    public DbSet<Citation> Citations { get; set; }

    public DbSet<CitationOccurrence> CitationOccurrences { get; set; }

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
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<ApplicationRole>().ToTable("roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<ApplicationUserRole>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        modelBuilder.Entity<Plan>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<PlanOption>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Order>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Subscription>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Payment>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Subject>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Membership>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<SubjectStorageConfiguration>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<SubjectAiConfiguration>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<GlobalAiConfiguration>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Chapter>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Document>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Document>().Property(e => e.StorageMethod).HasDefaultValue(DocumentStorageMethod.Unspecified);
        modelBuilder.Entity<DocumentChapter>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<DocumentComment>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ParsedSection>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Chunk>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ChatSession>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ChatSessionTitleGenerationSettings>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ChatSessionTitleGenerationMetrics>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ChatMessage>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ChatMessageGenerationSettings>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<ChatMessageGenerationMetrics>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Citation>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<CitationOccurrence>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<TestQuestion>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Experiment>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<TestResponse>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        modelBuilder.Entity<ApplicationUserRole>()
            .HasKey(e => new { e.UserId, e.RoleId });
        modelBuilder.Entity<ApplicationUserRole>()
            .HasIndex(e => e.UserId)
            .IsUnique();
        modelBuilder.Entity<ApplicationUser>()
            .HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity<ApplicationUserRole>(
                r => r.HasOne(j => j.Role)
                      .WithMany(p => p.UserRoles)
                      .HasForeignKey(j => j.RoleId),
                l => l.HasOne(j => j.User)
                      .WithMany(p => p.UserRoles)
                      .HasForeignKey(j => j.UserId));

        modelBuilder.Entity<Plan>()
            .HasIndex(e => e.Tier)
            .IsUnique();

        modelBuilder.Entity<PlanOption>()
            .HasIndex(e => new { e.PlanId, e.DurationDays })
            .IsUnique();

        modelBuilder.Entity<Subject>()
            .HasIndex(e => e.Code)
            .IsUnique();

        modelBuilder.Entity<Chapter>()
            .HasIndex(e => new { e.SubjectId, e.ChapterNumber })
            .IsUnique();

        modelBuilder.Entity<SubjectStorageConfiguration>()
            .HasOne(d => d.Subject)
            .WithOne(p => p.StorageConfiguration)
            .HasForeignKey<SubjectStorageConfiguration>(d => d.Id);

        modelBuilder.Entity<SubjectAiConfiguration>()
            .HasOne(d => d.Subject)
            .WithOne(p => p.AiConfiguration)
            .HasForeignKey<SubjectAiConfiguration>(d => d.Id);

        modelBuilder.Entity<Membership>()
            .HasIndex(e => new { e.SubjectId, e.UserId })
            .IsUnique();
        modelBuilder.Entity<Membership>()
            .HasIndex(e => new { e.SubjectId, e.Role })
            .HasFilter($"\"role\" = {(int)MembershipRole.Chief}")
            .IsUnique();
        modelBuilder.Entity<Subject>()
            .HasMany(e => e.Members)
            .WithMany(e => e.AssignedSubjects)
            .UsingEntity<Membership>(
                r => r.HasOne(j => j.User)
                      .WithMany(p => p.Memberships)
                      .HasForeignKey(j => j.UserId),
                l => l.HasOne(j => j.Subject)
                      .WithMany(p => p.Memberships)
                      .HasForeignKey(j => j.SubjectId));

        modelBuilder.Entity<DocumentChapter>()
            .HasIndex(e => new { e.DocumentId, e.ChapterId })
            .IsUnique();
        modelBuilder.Entity<Document>()
            .HasMany(e => e.Chapters)
            .WithMany(e => e.Documents)
            .UsingEntity<DocumentChapter>(
                r => r.HasOne(j => j.Chapter)
                      .WithMany(p => p.DocumentChapters)
                      .HasForeignKey(j => new { j.ChapterId, j.SubjectId })
                      .HasPrincipalKey(p => new { p.Id, p.SubjectId }),
                l => l.HasOne(j => j.Document)
                      .WithMany(p => p.DocumentChapters)
                      .HasForeignKey(j => new { j.DocumentId, j.SubjectId })
                      .HasPrincipalKey(p => new { p.Id, p.SubjectId }));

        modelBuilder.Entity<Chunk>()
            .Property(e => e.Embedding)
            .HasColumnType("vector(1024)");
        modelBuilder.Entity<Chunk>()
            .HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 32)
            .HasStorageParameter("ef_construction", 128);

        modelBuilder.Entity<ChatSessionTitleGenerationSettings>()
            .HasOne(d => d.ChatSession)
            .WithOne(p => p.TitleGenerationSettings)
            .HasForeignKey<ChatSessionTitleGenerationSettings>(d => d.Id);

        modelBuilder.Entity<ChatSessionTitleGenerationMetrics>()
            .HasOne(d => d.ChatSession)
            .WithOne(p => p.TitleGenerationMetrics)
            .HasForeignKey<ChatSessionTitleGenerationMetrics>(d => d.Id);

        modelBuilder.Entity<ChatMessageGenerationSettings>()
            .HasOne(d => d.ChatMessage)
            .WithOne(p => p.GenerationSettings)
            .HasForeignKey<ChatMessageGenerationSettings>(d => d.Id);

        modelBuilder.Entity<ChatMessageGenerationMetrics>()
            .HasOne(d => d.ChatMessage)
            .WithOne(p => p.GenerationMetrics)
            .HasForeignKey<ChatMessageGenerationMetrics>(d => d.Id);
    }
}
