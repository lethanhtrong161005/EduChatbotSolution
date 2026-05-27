using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace DataAccessLayer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // public DbSet<Subject> Subjects { get; set; }
        // public DbSet<Document> Documents { get; set; }
        // public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder.HasPostgresExtension("vector");

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
}