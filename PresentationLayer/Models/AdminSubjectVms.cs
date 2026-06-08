using Domain.Common;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

/// <summary>
/// ViewModel representing the paginated subject list page.
/// </summary>
public class AdminSubjectListVm
{
    public string? CodeFilter { get; set; }
    public string? NameFilter { get; set; }
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
    public PaginatedList<Subject> Subjects { get; set; } = null!;
}

/// <summary>
/// Request payload for creating a Subject.
/// </summary>
public class AdminCreateSubjectVm
{
    [Required(ErrorMessage = "Mã môn học không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã môn học không được vượt quá 50 ký tự.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn học không được để trống.")]
    [StringLength(255, ErrorMessage = "Tên môn học không được vượt quá 255 ký tự.")]
    public string SubjectName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>
/// Request payload for updating a Subject.
/// </summary>
public class AdminUpdateSubjectVm
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã môn học không được để trống.")]
    [StringLength(50, ErrorMessage = "Mã môn học không được vượt quá 50 ký tự.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn học không được để trống.")]
    [StringLength(255, ErrorMessage = "Tên môn học không được vượt quá 255 ký tự.")]
    public string SubjectName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>
/// Request payload for creating a Chapter.
/// </summary>
public class AdminCreateChapterVm
{
    [Required]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Tên chương không được để trống.")]
    [StringLength(255, ErrorMessage = "Tên chương không được vượt quá 255 ký tự.")]
    public string ChapterName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự chương phải lớn hơn hoặc bằng 1.")]
    public int? ChapterNumber { get; set; }
}

/// <summary>
/// Request payload for updating a Chapter.
/// </summary>
public class AdminUpdateChapterVm
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên chương không được để trống.")]
    [StringLength(255, ErrorMessage = "Tên chương không được vượt quá 255 ký tự.")]
    public string ChapterName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự chương phải lớn hơn hoặc bằng 1.")]
    public int? ChapterNumber { get; set; }
}

/// <summary>
/// Request payload for assigning a member to a subject.
/// </summary>
public class AdminAssignMemberVm
{
    [Required(ErrorMessage = "Không tìm thấy thông tin người dùng.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Vai trò thành viên không được để trống.")]
    public string Role { get; set; } = string.Empty; // Student, Lecturer, Chief
}

/// <summary>
/// DTO representing a member item returned to the client.
/// </summary>
public record SubjectMemberItemDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    DateTime AssignedAt
);

/// <summary>
/// DTO representing an eligible user for assignment returned to the client.
/// </summary>
public record EligibleUserItemDto(
    Guid UserId,
    string FullName,
    string Email
);
