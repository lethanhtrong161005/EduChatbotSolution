using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Business.Services.Account;

/// <summary>
/// Implements admin-level user management operations: paginated listing, creation,
/// update, soft-delete, disable, and reactivation. Optimistic-concurrency protection
/// is applied to all mutating operations via the <c>UpdatedAt</c> timestamp column.
/// </summary>
public class UserManagementService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IEmailService emailService,
    IEmailVerificationService emailVerificationService,
    IConfiguration configuration,
    IUnitOfWork unitOfWork) : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly IEmailService _emailService = emailService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly string _contactEmail = configuration["Email:SenderEmail"] ?? "support@educhatai.com";
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // ── READ ─────────────────────────────────────────────────────

    public async Task<UserManagementItemDto> GetUserAsync(Expression<Func<ApplicationUser, bool>> filter)
    {
        var query = _userManager.Users.AsNoTracking();

        query = query.Where(filter);

        var result = await query.ToListAsync();

        var user = result.FirstOrDefault()
                   ?? throw new EntityNotFoundException("No user matched the provided ID.");

        return new UserManagementItemDto
        (
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            await GetUserPrimaryRole(user),
            user.IsActive,
            user.DeletedAt.HasValue,
            user.UpdatedAt,
            user.DeletedAt
        );
    }

    private async Task<string> GetUserPrimaryRole(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(nameof(UserRole.Admin)))
            return nameof(UserRole.Admin);
        else if (roles.Contains(nameof(UserRole.Lecturer)))
            return nameof(UserRole.Lecturer);
        else
            return nameof(UserRole.Student);
    }

    /// <summary>
    /// Returns a paginated list of all users filtered by optional name, email, and role.
    /// Soft-deleted accounts are included and flagged with <c>IsDeleted = true</c>.
    /// </summary>
    /// <param name="nameFilter">Optional substring match on full name (case-insensitive).</param>
    /// <param name="emailFilter">Optional substring match on email (case-insensitive).</param>
    /// <param name="roleFilter">Optional exact role name match.</param>
    /// <param name="limit">Page size (max records per page).</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <returns>A <see cref="PaginatedList{T}"/> of <see cref="UserManagementItemDto"/>.</returns>
    public async Task<PaginatedList<UserManagementItemDto>> GetPagedUsersAsync(
        string? nameFilter,
        string? emailFilter,
        string? roleFilter,
        int limit,
        int offset)
    {
        // 1. Build base query — order by creation date descending
        var query = _userManager.Users.AsNoTracking();

        // 2. Apply filters
        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(u => u.FullName.ToLower().Contains(nameFilter.ToLower()));

        if (!string.IsNullOrWhiteSpace(emailFilter))
            query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(emailFilter.ToLower()));

        // 3. Count before pagination
        var totalCount = await query.CountAsync();

        // 4. Paginate
        var users = await query
            .OrderByDescending(u => u.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        // 5. Load roles for each user (Identity doesn't support JOIN in EF for roles)
        var dtos = new List<UserManagementItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Unknown";

            // Apply role filter after role lookup
            if (!string.IsNullOrWhiteSpace(roleFilter) &&
                !string.Equals(primaryRole, roleFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            dtos.Add(new UserManagementItemDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                primaryRole,
                user.IsActive,
                user.DeletedAt.HasValue,
                user.UpdatedAt,
                user.DeletedAt
            ));
        }

        // Recalculate total for role-filtered scenarios
        var effectiveTotal = string.IsNullOrWhiteSpace(roleFilter) ? totalCount : dtos.Count;

        return new PaginatedList<UserManagementItemDto>(
            dtos, effectiveTotal, limit > 0 ? limit : 10, (offset / (limit > 0 ? limit : 10)) + 1);
    }

    /// <summary>
    /// Returns all role names from the database for the role selector dropdown.
    /// </summary>
    public async Task<IList<string>> GetAllRolesAsync()
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .Select(r => r.Name!)
            .ToListAsync();
    }

    // ── CREATE ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new active user in the database with the given role, marks the email as confirmed,
    /// then sends the login credentials to the user's email address.
    /// </summary>
    /// <param name="dto">Creation data: full name, email, password, and role.</param>
    /// <returns>Success/error tuple. <c>Error</c> is null on success.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required fields are missing.</exception>
    public async Task<(bool Success, ApplicationUser? user, string? Error)> CreateUserAsync(CreateUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 0. Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Email))
            return (false, null, "Email is required.");
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return (false, null, "Full name is required.");
        if (string.IsNullOrWhiteSpace(dto.Role))
            return (false, null, "Role is required.");

        // 1. Check email uniqueness
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return (false, null, "An account with this email address already exists.");

        // 1b. Check username uniqueness (username = email normalized)
        if (await _userManager.FindByNameAsync(dto.Email) is not null)
            return (false, null, "An account with this email address already exists (username conflict).");

        // 2. Validate role exists in DB
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return (false, null, $"Role '{dto.Role}' does not exist.");

        // 3. Auto-generate a cryptographically secure password (12 chars)
        var generatedPassword = GenerateSecurePassword(12);

        // 4. Create active user in database; admin-created accounts are trusted but
        //    must change the auto-generated password on first login.
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            NormalizedUserName = dto.Email.ToUpperInvariant(),
            Email = dto.Email,
            NormalizedEmail = dto.Email.ToUpperInvariant(),
            FullName = dto.FullName,
            EmailConfirmed = true,
            IsActive = true,
            MustChangePassword = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword),
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return (false, null, createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user.");

        // 5. Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!roleResult.Succeeded)
            return (false, null, roleResult.Errors.FirstOrDefault()?.Description ?? "Failed to assign role.");

        // 6. Add identity claims
        await _userManager.AddClaimsAsync(user,
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, dto.Role),
        ]);

        // 7. Send credentials email. Roll back the active account if delivery fails.
        try
        {
            await _emailService.SendAdminCreatedCredentialsAsync(dto.Email, dto.FullName, generatedPassword);
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);
            return (false, null, $"Failed to send account credentials email: {ex.Message}");
        }

        return (true, user, null);
    }

    /// <summary>
    /// Generates a cryptographically secure random password containing uppercase letters,
    /// lowercase letters, digits, and special characters.
    /// </summary>
    /// <param name="length">The desired password length (minimum 8).</param>
    /// <returns>A random password string.</returns>
    private static string GenerateSecurePassword(int length)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*?";
        const string allChars = upper + lower + digits + special;

        var password = new char[length];
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        // Guarantee at least one character from each category
        password[0] = upper[GetRandomIndex(rng, upper.Length)];
        password[1] = lower[GetRandomIndex(rng, lower.Length)];
        password[2] = digits[GetRandomIndex(rng, digits.Length)];
        password[3] = special[GetRandomIndex(rng, special.Length)];

        // Fill remaining positions with random characters from all categories
        for (var i = 4; i < length; i++)
        {
            password[i] = allChars[GetRandomIndex(rng, allChars.Length)];
        }

        // Shuffle to avoid predictable positions for category-guaranteed chars
        Shuffle(rng, password);

        return new string(password);
    }

    private static int GetRandomIndex(System.Security.Cryptography.RandomNumberGenerator rng, int maxExclusive)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return (int)(BitConverter.ToUInt32(bytes, 0) % (uint)maxExclusive);
    }

    private static void Shuffle(System.Security.Cryptography.RandomNumberGenerator rng, char[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = GetRandomIndex(rng, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // ── UPDATE ────────────────────────────────────────────────────

    /// <summary>
    /// Updates a user's profile. Saves changes to database immediately, including email changes.
    /// If email changes, sets EmailConfirmed = false and sends verification email asynchronously.
    /// </summary>
    /// <param name="dto">Update data with the current <c>UpdatedAt</c> timestamp from the UI.</param>
    /// <returns>Success/error tuple. Returns a 409-style error on concurrency conflict.</returns>
    public async Task<(bool Success, ApplicationUser? user, string? Error)> UpdateUserAsync(UpdateUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 1. Load user
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user is null)
            return (false, null, "User not found.");

        // 2. Optimistic concurrency check
        if (user.UpdatedAt != dto.UpdatedAt)
            return (false, null, "This record was modified by another administrator. Please refresh and try again.");

        // 3. Detect email change
        var emailChanged = !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            // Check new email not already in use
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null && existing.Id != user.Id)
                return (false, null, "The new email address is already in use by another account.");

            // Update email and set EmailConfirmed = false (will be confirmed after verification)
            user.Email = dto.Email;
            user.NormalizedEmail = dto.Email.ToUpperInvariant();
            user.UserName = dto.Email;
            user.NormalizedUserName = dto.Email.ToUpperInvariant();
            user.EmailConfirmed = false;
        }

        // 4. Update profile fields
        user.FullName = dto.FullName;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // 5. Update role
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(dto.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);

            // Sync role claim
            var roleClaims = (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == ClaimTypes.Role)
                .ToList();
            await _userManager.RemoveClaimsAsync(user, roleClaims);
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, dto.Role));
        }

        // 6. Save all changes to database
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, null, result.Errors.FirstOrDefault()?.Description ?? "Failed to update user.");

        // 7. If email changed, send verification email (async, non-blocking)
        if (emailChanged)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailVerificationService.InitiateEmailUpdateVerificationAsync(
                        dto.Email, dto.FullName, user.Id);
                }
                catch (Exception ex)
                {
                    // Log error but don't block the update
                    System.Diagnostics.Debug.WriteLine($"Failed to send email verification: {ex.Message}");
                }
            });
        }

        return (true, user, null);
    }

    // ── SOFT DELETE ───────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a user by setting <c>DeletedAt</c>. Sends a deletion notification email.
    /// Disabled accounts are also eligible for soft-deletion.
    /// </summary>
    /// <param name="userId">The user's database ID.</param>
    /// <returns>Success/error tuple.</returns>
    public async Task<(bool Success, ApplicationUser? user, string? Error)> SoftDeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, null, "User not found.");

        if (user.DeletedAt.HasValue)
            return (false, null, "This account has already been deleted.");

        // 1. Soft-delete
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, null, result.Errors.FirstOrDefault()?.Description ?? "Failed to delete user.");

        // 2. Send notification email (fire-and-forget on failure — do not block deletion)
        try
        {
            await _emailService.SendAccountDeletedAsync(
                user.Email ?? string.Empty, user.FullName, _contactEmail);
        }
        catch
        {
            // Email failure is non-critical; deletion is already committed
        }

        return (true, user, null);
    }

    // ── DISABLE ───────────────────────────────────────────────────

    /// <summary>
    /// Disables a user account. Applies optimistic-concurrency check.
    /// Sends a disable-notification email to the user.
    /// </summary>
    /// <param name="userId">The user's database ID.</param>
    /// <param name="updatedAt">The <c>UpdatedAt</c> timestamp from the client for concurrency checking.</param>
    /// <returns>Success/error tuple.</returns>
    public async Task<(bool Success, ApplicationUser? user, string? Error)> DisableUserAsync(Guid userId, DateTimeOffset updatedAt)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, null, "User not found.");

        // 1. Optimistic concurrency check
        if (user.UpdatedAt != updatedAt)
            return (false, null, "This record was modified by another administrator. Please refresh and try again.");

        if (!user.IsActive)
            return (false, null, "The account is already disabled.");

        // 2. Disable
        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, null, result.Errors.FirstOrDefault()?.Description ?? "Failed to disable account.");

        // 3. Send notification email
        try
        {
            await _emailService.SendAccountDisabledAsync(
                user.Email ?? string.Empty, user.FullName, _contactEmail);
        }
        catch
        {
            // Email failure is non-critical
        }

        return (true, user, null);
    }

    // ── REACTIVATE ────────────────────────────────────────────────

    /// <summary>
    /// Re-enables a disabled user account. Applies optimistic-concurrency check.
    /// </summary>
    /// <param name="userId">The user's database ID.</param>
    /// <param name="updatedAt">The <c>UpdatedAt</c> timestamp from the client for concurrency checking.</param>
    /// <returns>Success/error tuple.</returns>
    public async Task<(bool Success, ApplicationUser? user, string? Error)> ReactivateUserAsync(Guid userId, DateTimeOffset updatedAt)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, null, "User not found.");

        // 1. Optimistic concurrency check
        if (user.UpdatedAt != updatedAt)
            return (false, null, "This record was modified by another administrator. Please refresh and try again.");

        if (user.IsActive)
            return (false, null, "The account is already active.");

        if (user.DeletedAt.HasValue)
            return (false, null, "Cannot reactivate a deleted account.");

        // 2. Reactivate
        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, null, result.Errors.FirstOrDefault()?.Description ?? "Failed to reactivate account.");

        return (true, user, null);
    }

    // ── EXCEL IMPORT ────────────────────────────────────────────────
    
    public async Task<UserImportBatch> CreateImportBatchAsync(Guid importedBy, string fileName, string storageLocator)
    {
        var batch = new UserImportBatch
        {
            FileName = fileName,
            StorageLocator = storageLocator,
            ImportedById = importedBy,
            TotalRows = 0,
            ProcessedRows = 0,
            Status = ImportBatchStatus.Pending
        };

        await _unitOfWork.UserImportBatches.InsertAsync(batch);
        await _unitOfWork.SaveAsync();
        return batch;
    }

    public async Task<UserImportValidationResult> ParseAndValidateImportBatchAsync(Guid batchId, Stream fileStream)
    {
        var batch = (await _unitOfWork.UserImportBatches.GetAsync(
            filter: b => b.Id == batchId,
            includeProperties: ["Rows"])).FirstOrDefault();

        if (batch == null)
            throw new EntityNotFoundException("Import batch not found.");

        var errors = new List<string>();
        var validRows = new List<UserImportRowDto>();

        try
        {
            using var document = SpreadsheetDocument.Open(fileStream, false);
            var workbookPart = document.WorkbookPart;
            if (workbookPart == null)
            {
                errors.Add("Invalid Excel file format.");
                return new UserImportValidationResult(false, errors, validRows);
            }

            if (workbookPart.Workbook == null)
            {
                errors.Add("Invalid Excel file format: missing workbook.");
                return new UserImportValidationResult(false, errors, validRows);
            }

            var sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault();
            if (sheet == null || sheet.Id == null)
            {
                errors.Add("No sheet found in the Excel file.");
                return new UserImportValidationResult(false, errors, validRows);
            }

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            if (worksheetPart.Worksheet == null)
            {
                errors.Add("Invalid Excel file format: missing worksheet.");
                return new UserImportValidationResult(false, errors, validRows);
            }
            
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null)
            {
                errors.Add("Invalid Excel file format: missing sheet data.");
                return new UserImportValidationResult(false, errors, validRows);
            }

            var rows = sheetData.Elements<Row>().ToList();
            if (rows.Count <= 1)
            {
                errors.Add("The Excel file is empty or only contains a header.");
                return new UserImportValidationResult(false, errors, validRows);
            }

            if (rows.Count > 501) // Header + 500 rows max
            {
                errors.Add("The Excel file exceeds the maximum allowed 500 rows.");
                return new UserImportValidationResult(false, errors, validRows);
            }

            var sharedStringTable = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault()?.SharedStringTable;

            string GetCellValue(Cell cell)
            {
                if (cell.CellValue == null) return string.Empty;
                string value = cell.CellValue.InnerXml;
                if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                {
                    return sharedStringTable?.ElementAt(int.Parse(value))?.InnerText ?? string.Empty;
                }
                return value;
            }

            var emailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var cells = row.Elements<Cell>().ToList();
                
                string fullName = cells.Count > 0 ? GetCellValue(cells[0]).Trim() : string.Empty;
                string email = cells.Count > 1 ? GetCellValue(cells[1]).Trim() : string.Empty;
                string role = cells.Count > 2 ? GetCellValue(cells[2]).Trim() : string.Empty;

                bool isRowValid = true;

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    errors.Add($"Row {i + 1}: Full Name is required.");
                    isRowValid = false;
                }

                if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                {
                    errors.Add($"Row {i + 1}: Valid Email is required.");
                    isRowValid = false;
                }
                else if (!emailSet.Add(email))
                {
                    errors.Add($"Row {i + 1}: Duplicate email '{email}' within the file.");
                    isRowValid = false;
                }

                if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
                {
                    errors.Add($"Row {i + 1}: Invalid or missing role '{role}'. Expected roles: Admin, Student, Lecturer.");
                    isRowValid = false;
                }
                
                if (isRowValid)
                {
                    validRows.Add(new UserImportRowDto(i + 1, fullName, email, role));
                }
            }

            if (validRows.Any())
            {
                foreach (var row in validRows)
                {
                    var entityRow = new UserImportRow
                    {
                        BatchId = batch.Id,
                        RowNumber = row.RowNumber,
                        FullName = row.FullName,
                        Email = row.Email,
                        Role = row.Role,
                        Status = ImportRowStatus.Pending
                    };
                    batch.Rows.Add(entityRow);
                }
                batch.TotalRows = validRows.Count;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to process Excel file: {ex.Message}");
        }

        await _unitOfWork.SaveAsync();
        return new UserImportValidationResult(errors.Count == 0, errors, validRows);
    }

    public async Task ProcessImportBatchRowAsync(Guid batchId, Guid rowId)
    {
        var row = await _unitOfWork.UserImportRows.FindByIdAsync(rowId);
        if (row == null || row.BatchId != batchId || row.Status != ImportRowStatus.Pending)
        {
            return;
        }

        try
        {
            var dto = new CreateUserDto(row.FullName, row.Email, row.Role);
            var result = await CreateUserAsync(dto);

            if (result.Success)
            {
                row.Status = ImportRowStatus.Success;
                row.CreatedUserId = result.user?.Id;
            }
            else
            {
                row.Status = ImportRowStatus.Failed;
                row.ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            row.Status = ImportRowStatus.Failed;
            row.ErrorMessage = ex.Message;
        }

        row.ProcessedAt = DateTimeOffset.UtcNow;
        _unitOfWork.UserImportRows.Update(row);
        await _unitOfWork.SaveAsync();
    }

    public async Task<PaginatedList<UserImportBatchSummaryDto>> GetImportHistoryAsync(int limit, int offset, string? fileName = null)
    {
        var pageSize = limit > 0 ? limit : 10;
        var pageIndex = (offset / pageSize) + 1;

        System.Linq.Expressions.Expression<Func<UserImportBatch, bool>>? filter = null;
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            filter = b => b.FileName.Contains(fileName);
        }

        var batches = await _unitOfWork.UserImportBatches.GetAsync(
            filter: filter,
            orderBy: q => q.OrderByDescending(b => b.CreatedAt),
            paginationSettings: (pageSize, pageIndex)
        );
        
        var totalCount = await _unitOfWork.UserImportBatches.CountAsync(filter);
        
        var summaryDtos = batches.Select(b => new UserImportBatchSummaryDto(
            b.Id,
            b.FileName,
            b.TotalRows,
            b.ProcessedRows,
            b.SuccessRows,
            b.FailedRows,
            b.Status,
            b.ImportedBy != null ? b.ImportedBy.FullName : "Unknown",
            b.CreatedAt,
            b.CompletedAt
        )).ToList();
            
        return new PaginatedList<UserImportBatchSummaryDto>(summaryDtos, totalCount, pageSize, pageIndex);
    }

    public async Task<UserImportBatchDetailDto> GetImportBatchDetailAsync(Guid batchId)
    {
        var batches = await _unitOfWork.UserImportBatches.GetAsync(
            filter: b => b.Id == batchId
        );
        var batch = batches.FirstOrDefault();

        if (batch == null)
        {
            throw new EntityNotFoundException("Import batch not found.");
        }

        var summary = new UserImportBatchSummaryDto(
            batch.Id,
            batch.FileName,
            batch.TotalRows,
            batch.ProcessedRows,
            batch.SuccessRows,
            batch.FailedRows,
            batch.Status,
            batch.ImportedBy != null ? batch.ImportedBy.FullName : "Unknown",
            batch.CreatedAt,
            batch.CompletedAt
        );

        return new UserImportBatchDetailDto(summary, []);
    }

    public async Task<PaginatedList<UserImportRowDetailDto>> GetImportBatchRowsAsync(Guid batchId, int limit, int offset, string? email = null)
    {
        var pageSize = limit > 0 ? limit : 10;
        var pageIndex = (offset / pageSize) + 1;

        System.Linq.Expressions.Expression<Func<UserImportRow, bool>> filter;
        if (!string.IsNullOrWhiteSpace(email))
        {
            filter = r => r.BatchId == batchId && r.Email.Contains(email);
        }
        else
        {
            filter = r => r.BatchId == batchId;
        }

        var rows = await _unitOfWork.UserImportRows.GetAsync(
            filter: filter,
            orderBy: q => q.OrderBy(r => r.RowNumber),
            paginationSettings: (pageSize, pageIndex)
        );

        var totalCount = await _unitOfWork.UserImportRows.CountAsync(filter);

        var dtos = rows.Select(r => new UserImportRowDetailDto(
            r.RowNumber,
            r.FullName,
            r.Email,
            r.Role,
            r.Status,
            r.ErrorMessage,
            r.ProcessedAt
        )).ToList();

        return new PaginatedList<UserImportRowDetailDto>(dtos, totalCount, pageSize, pageIndex);
    }
}
