namespace Presentation.ViewModels;

public class ErrorVm
{
    public string? RequestId { get; set; }
    public string? Title { get; set; } = "Error";
    public string? Detail { get; set; } = "An error has occurred.";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
