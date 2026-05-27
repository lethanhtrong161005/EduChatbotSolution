using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

public class EduChatbotDbContext : DbContext
{
    // public DbSet<Subject> Subjects { get; set; }
    // public DbSet<Document> Documents { get; set; }
    // public DbSet<DocumentChunk> DocumentChunks { get; set; }

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

        base.OnModelCreating(modelBuilder);
    }


}