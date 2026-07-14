using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;
using Hangfire;
using Supabase.Storage.Interfaces;
using Presentation.Background;
using Domain.Exceptions;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator user management page with filters and pagination.
/// Handles page display and AJAX user management endpoints.
/// </summary>
[Authorize(Roles = "Admin")]
public class UserManageModel(
    IUserManagementService userManagementService,
    IResourceRealtimeNotifier notifier)
    : PageModel
{
    private readonly IUserManagementService _userManagementService = userManagementService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;

    /// <summary>
    /// Gets the user management view model rendered by the page.
    /// </summary>
    public AdminUserListVm ViewModel { get; private set; } = new();

    [FromHeader]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads users and roles for the user management page.
    /// </summary>
    /// <param name="name">Optional name filter.</param>
    /// <param name="email">Optional email filter.</param>
    /// <param name="role">Optional role filter.</param>
    /// <param name="limit">The requested page size.</param>
    /// <param name="offset">The zero-based record offset.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync(string? name, string? email, string? role, int limit = 10, int offset = 0)
    {
        var users = await _userManagementService.GetPagedUsersAsync(name, email, role, limit, offset);
        var roles = await _userManagementService.GetAllRolesAsync();

        ViewModel = new AdminUserListVm
        {
            NameFilter = name,
            EmailFilter = email,
            RoleFilter = role,
            Limit = limit,
            Offset = offset,
            Users = users,
            AvailableRoles = roles,
        };
    }

    public async Task<IActionResult> OnGetGetUsersAsync(string? name, string? email, string? role, int limit = 10, int offset = 0)
    {
        var users = await _userManagementService.GetPagedUsersAsync(name, email, role, limit, offset);

        var dto = new
        {
            Users = users,
            TotalCount = users.Count,
        };

        return new JsonResult(dto);
    }

    /// <summary>
    /// Creates a new active user account and emails the initial login credentials.
    /// Returns JSON so the page can show inline success/error without a full reload.
    /// </summary>
    /// <param name="vm">Create-user form data submitted via AJAX.</param>
    public async Task<IActionResult> OnPostCreateUserAsync([FromBody] AdminCreateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        var (success, user, error) = await _userManagementService.CreateUserAsync(
            new CreateUserDto(vm.FullName, vm.Email, vm.Role));

        if (!success)
            return StatusCode(500, new { success, error });

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Created,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }

    /// <summary>
    /// Updates a user's profile (name, email, role). Applies optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="vm">Update form data.</param>
    public async Task<IActionResult> OnPutUpdateUserAsync([FromQuery] Guid id, [FromBody] AdminUpdateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        if (id != vm.UserId)
            return BadRequest(new { success = false, error = "User ID mismatch." });

        var (success, user, error) = await _userManagementService.UpdateUserAsync(
            new UpdateUserDto(vm.UserId, vm.FullName, vm.Email, vm.Role, vm.UpdatedAt));

        if (!success)
            return StatusCode(500, new { success, error });

        var message = vm.Email != vm.OriginalEmail
            ? "Changes saved! A verification email has been sent to the new email address."
            : "Changes saved successfully!";

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Updated,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, message });
    }

    // ── Excel Import ──────────────────────────────────────────────

    /// <summary>
    /// Uploads an Excel file, validates it, stores it, and enqueues a background job.
    /// </summary>
    public async Task<IActionResult> OnPostImportUsersAsync(
        IFormFile file,
        [FromServices] IBackgroundJobClient backgroundJobs,
        [FromServices] Supabase.Client supabase)
    {
        if (file == null || file.Length == 0)
        {
            return new JsonResult(new { success = false, message = "Please select a valid Excel file." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return new JsonResult(new { success = false, message = "File size must not exceed 5MB." });
        }

        var allowedExtensions = new[] { ".xlsx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return new JsonResult(new { success = false, message = "Invalid file type. Only .xlsx files are supported." });
        }

        // Store file in Supabase
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var fileId = Guid.NewGuid();
        var storageLocator = "";

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset position for reading
            
            var storageStrategy = HttpContext.RequestServices.GetRequiredKeyedService<Domain.Contracts.IDurableStorageStrategy>(Domain.Entities.DocumentStorageMethod.Supabase);
            var storeResult = await storageStrategy.StoreAsync(memoryStream, $"{fileId}{extension}", Domain.Contracts.DocumentFileDirectory.Received, CancellationToken.None);
            
            if (!storeResult.Success)
            {
                return new JsonResult(new { success = false, message = $"Failed to upload file to storage: {string.Join(", ", storeResult.Errors ?? Array.Empty<string>())}" });
            }
            
            storageLocator = storeResult.Locator;
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Failed to upload file to storage: {ex.Message}" });
        }

        var batch = await _userManagementService.CreateImportBatchAsync(userId, file.FileName, storageLocator!);

        backgroundJobs.Enqueue<UserImportJob>(job => job.ParseAsync(batch.Id, CancellationToken.None));

        return new JsonResult(new { success = true, batchId = batch.Id });
    }

    /// <summary>
    /// Downloads an Excel template for user import.
    /// </summary>
    public IActionResult OnGetDownloadTemplate()
    {
        using var stream = new MemoryStream();
        using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

            var worksheetPart = workbookPart.AddNewPart<DocumentFormat.OpenXml.Packaging.WorksheetPart>();
            var sheetData = new DocumentFormat.OpenXml.Spreadsheet.SheetData();
            worksheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(sheetData);

            var sheets = document.WorkbookPart!.Workbook!.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Sheets());
            var sheet = new DocumentFormat.OpenXml.Spreadsheet.Sheet() { Id = document.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Users" };
            sheets.Append(sheet);

            var headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
            headerRow.Append(
                new DocumentFormat.OpenXml.Spreadsheet.Cell { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("Full Name"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String },
                new DocumentFormat.OpenXml.Spreadsheet.Cell { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("Email"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String },
                new DocumentFormat.OpenXml.Spreadsheet.Cell { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("Role"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String }
            );
            sheetData.Append(headerRow);

            var sampleRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
            sampleRow.Append(
                new DocumentFormat.OpenXml.Spreadsheet.Cell { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("John Doe"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String },
                new DocumentFormat.OpenXml.Spreadsheet.Cell { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("john.doe@example.com"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String },
                new DocumentFormat.OpenXml.Spreadsheet.Cell { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("Student"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String }
            );
            sheetData.Append(sampleRow);

            workbookPart.Workbook.Save();
        }

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserImportTemplate.xlsx");
    }

    /// <summary>
    /// Gets the import history for display in the UI.
    /// </summary>
    public async Task<IActionResult> OnGetImportHistoryAsync(int limit = 10, int offset = 0, string? fileName = null)
    {
        var history = await _userManagementService.GetImportHistoryAsync(limit, offset, fileName);
        return new JsonResult(new
        {
            items = history,
            totalCount = history.TotalCount,
            pageSize = history.PageSize,
            pageIndex = history.PageIndex
        });
    }

    /// <summary>
    /// Gets the details of a specific import batch.
    /// </summary>
    public async Task<IActionResult> OnGetImportBatchDetailAsync(Guid batchId)
    {
        try
        {
            var detail = await _userManagementService.GetImportBatchDetailAsync(batchId);
            return new JsonResult(detail, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Gets a paginated list of rows for a specific import batch.
    /// </summary>
    public async Task<IActionResult> OnGetImportBatchRowsAsync(Guid batchId, int limit = 10, int offset = 0, string? email = null)
    {
        var rows = await _userManagementService.GetImportBatchRowsAsync(batchId, limit, offset, email);
        return new JsonResult(new
        {
            items = rows,
            totalCount = rows.TotalCount,
            pageSize = rows.PageSize,
            pageIndex = rows.PageIndex
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
    }

    /// <summary>
    /// Soft-deletes a user account. Sends a deletion-notification email.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    public async Task<IActionResult> OnDeleteDeleteUserAsync([FromQuery] Guid id)
    {
        var (success, user, error) = await _userManagementService.SoftDeleteUserAsync(id);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Deleted,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }

    /// <summary>
    /// Disables a user account using optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the concurrency token.</param>
    public async Task<IActionResult> OnPutDisableUserAsync([FromQuery] Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, user, error) = await _userManagementService.DisableUserAsync(id, body.UpdatedAt);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Disabled,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }

    /// <summary>
    /// Re-enables a previously disabled user account using optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the concurrency token.</param>
    public async Task<IActionResult> OnPutReactivateUserAsync([FromQuery] Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, user, error) = await _userManagementService.ReactivateUserAsync(id, body.UpdatedAt);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Enabled,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }
}
