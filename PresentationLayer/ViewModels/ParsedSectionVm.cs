namespace Presentation.ViewModels;

public class ParsedSectionVm
{
    public int SectionIndex { get; set; }
    public int? PageNumber { get; set; }
    public string? SectionTitle { get; set; }
    public string Text { get; set; } = string.Empty;
}
