using System.ComponentModel.DataAnnotations;

namespace Presentation.ViewModels;

public class DocumentEditVm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200, ErrorMessage = "Tiêu đề không quá 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự")]
    public string? Description { get; set; }
}
