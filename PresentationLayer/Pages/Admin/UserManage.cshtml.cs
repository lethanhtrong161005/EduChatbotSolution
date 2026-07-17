using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Domain.Utils;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Background;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator user management page with filters and pagination.
/// Handles page display and AJAX user management endpoints.
/// </summary>
[Authorize(Roles = "Admin")]
public class UserManageModel(
    IUserManagementService userManagementService,
    IUserImportFileReceptionService importReceptionService,
    IResourceRealtimeNotifier notifier,
    ILogger<UserManageModel> logger)
    : PageModel
{
    private readonly IUserManagementService _userManagementService = userManagementService;
    private readonly IUserImportFileReceptionService _importReceptionService = importReceptionService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly ILogger<UserManageModel> _logger = logger;

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
            return BadRequest(new { Success = false, Error = "Invalid form data." });

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
            return BadRequest(new { Success = false, Error = "Invalid form data." });

        if (id != vm.UserId)
            return BadRequest(new { Success = false, Error = "User ID mismatch." });

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

    // ── Excel Import ──────────────────────────────────────────────

    /// <summary>
    /// Uploads an Excel file, validates it, stores it, and enqueues a background job.
    /// </summary>
    public async Task<IActionResult> OnPostImportUsersAsync(IFormFile file, CancellationToken cxlTkn)
    {
        try
        {
            if (file == null || file.Length == 0)
                return new JsonResult(new { Success = false, message = "Please select a valid Excel file." });
            if (file.Length > 5L * 1024 * 1024)
                return new JsonResult(new { Success = false, message = "File size must not exceed 5MB." });

            // Store file in Supabase
            var userId = User.GetUserId();
            var stagingLocator = string.Empty;

            try
            {
                var stagingResult = await _importReceptionService.ReceiveAsync(file.OpenReadStream(), file.FileName, cxlTkn);
                if (!stagingResult.Success)
                    return StatusCode(StatusCodes.Status400BadRequest, new { Success = false, message = $"File was not accepted: {string.Join(", ", stagingResult.Errors)}" });

                stagingLocator = stagingResult.Locator;
            }
            catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to stage user-import file: {Error}", ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, new { Success = false, message = $"Failed to save file." });
            }

            var batch = await _userManagementService.CreateImportBatchAsync(userId, file.FileName, stagingLocator);

            var persistJobId = BackgroundJob.Enqueue<UserImportFilePersistenceJob>(job => job.PersistAsync(batch.Id, CancellationToken.None));
            BackgroundJob.ContinueJobWith<UserImportJob>(persistJobId, job => job.ImportAsync(batch.Id, CancellationToken.None));

            return new JsonResult(new { Success = true, batchId = batch.Id });
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
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

        return File(stream.ToArray(), FileHelper.GetMimeType(FileType.XLSX), "UserImportTemplate.xlsx");
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
}
