using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data;

public class EduChatbotDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Chunk> Chunks { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Citation> Citations { get; set; }
    public DbSet<TestQuestion> TestQuestions { get; set; }
    public DbSet<Experiment> Experiments { get; set; }
    public DbSet<TestResponse> TestResponses { get; set; }

    public EduChatbotDbContext() : base()
    {
    }

    public EduChatbotDbContext(DbContextOptions<EduChatbotDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        // modelBuilder.Entity<DocumentChunk>(entity =>
        // {
        //     entity.ToTable("DocumentChunks");
        //     entity.HasKey(e => e.Id);

        //     entity.Property(e => e.EmbeddingVector)
        //           .HasColumnType("vector(1536)");

        //     entity.HasOne(d => d.Document)
        //           .WithMany()
        //           .HasForeignKey(d => d.DocumentId)
        //           .OnDelete(DeleteBehavior.Cascade);
        // });

        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        base.OnModelCreating(modelBuilder);
    }
}
