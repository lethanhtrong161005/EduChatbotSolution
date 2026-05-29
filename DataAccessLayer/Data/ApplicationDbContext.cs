using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Data
{
    /// <summary>
    /// The Entity Framework Core database context for the EduChatAI application.
    /// Manages all database entities and their relationships with the PostgreSQL database.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext.
        /// </summary>
        /// <param name="options">Database context configuration options.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the DbSet for managing user accounts in the system.
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Gets or sets the DbSet for managing system roles.
        /// </summary>
        public DbSet<Role> Roles { get; set; } = null!;

        /// <summary>
        /// Gets or sets the DbSet for managing user-role assignments.
        /// </summary>
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        // public DbSet<Subject> Subjects { get; set; }
        // public DbSet<Document> Documents { get; set; }
        // public DbSet<DocumentChunk> DocumentChunks { get; set; }

        /// <summary>
        /// Configures the database schema and entity relationships.
        /// </summary>
        /// <param name="modelBuilder">The model builder to configure entities.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure pgvector extension
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("vector");

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.FullName)
                    .HasColumnName("full_name")
                    .HasColumnType("varchar(150)")
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasColumnType("varchar(255)")
                    .IsRequired();

                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_users_email");

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("password_hash")
                    .HasColumnType("text")
                    .IsRequired();

                entity.Property(e => e.IsEmailVerified)
                    .HasColumnName("is_email_verified")
                    .HasDefaultValue(false);

                entity.Property(e => e.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasMany(e => e.UserRoles)
                    .WithOne(ur => ur.User)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(50)")
                    .IsRequired();

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar(255)");

                entity.HasMany(e => e.UserRoles)
                    .WithOne(ur => ur.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserRole entity (Junction table)
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.RoleId)
                    .HasColumnName("role_id")
                    .HasColumnType("int");
            });

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