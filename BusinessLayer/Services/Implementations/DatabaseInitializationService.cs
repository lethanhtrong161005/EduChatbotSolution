using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using BusinessLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLayer.Services.Implementations;

/// <summary>
/// Service for initializing and seeding the database with default roles and test users.
/// Ensures database is properly set up on application startup.
/// </summary>
public class DatabaseInitializationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the DatabaseInitializationService.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="authService">The authentication service for password hashing.</param>
    public DatabaseInitializationService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Initializes the database by applying migrations and seeding default data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            // 1. Apply pending migrations
            await _context.Database.MigrateAsync();

            // 2. Seed default roles if they don't exist
            await SeedRolesAsync();

            // 3. Seed test users if they don't exist
            await SeedTestUsersAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with default system roles.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SeedRolesAsync()
    {
        // Check if roles already exist
        if (await _context.Roles.AnyAsync())
        {
            return;
        }

        var roles = new List<Role>
        {
            new Role
            {
                Name = "Admin",
                Description = "System administrator with full permissions"
            },
            new Role
            {
                Name = "Lecturer",
                Description = "Lecturer role for managing courses and content"
            },
            new Role
            {
                Name = "Student",
                Description = "Student role with basic access permissions"
            }
        };

        await _context.Roles.AddRangeAsync(roles);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the database with test user accounts for demonstration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SeedTestUsersAsync()
    {
        // Check if users already exist
        if (await _context.Users.AnyAsync())
        {
            return;
        }

        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        var lecturerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Lecturer");
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");

        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                FullName = "Admin User",
                Email = "admin@educhatai.com",
                PasswordHash = _authService.HashPassword("Admin@123456"),
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserRoles = adminRole != null ? new List<UserRole>
                {
                    new UserRole { RoleId = adminRole.Id }
                } : new List<UserRole>()
            },
            new User
            {
                Id = Guid.NewGuid(),
                FullName = "Lecturer Test",
                Email = "lecturer@educhatai.com",
                PasswordHash = _authService.HashPassword("Lecturer@123456"),
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserRoles = lecturerRole != null ? new List<UserRole>
                {
                    new UserRole { RoleId = lecturerRole.Id }
                } : new List<UserRole>()
            },
            new User
            {
                Id = Guid.NewGuid(),
                FullName = "Student Test",
                Email = "student@educhatai.com",
                PasswordHash = _authService.HashPassword("Student@123456"),
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserRoles = studentRole != null ? new List<UserRole>
                {
                    new UserRole { RoleId = studentRole.Id }
                } : new List<UserRole>()
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
    }
}
