namespace PresentationLayer.Options;

public class ErrorHandlingOptions
{
    public const string ProblemDetails = "ErrorMessage";

    public string ErrorPagePath { get; set; } = null!;
}
