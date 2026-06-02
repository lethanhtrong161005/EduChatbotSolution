namespace Presentation.Models;

public class PlanOptionCardVm
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public bool IsAvailable { get; set; } = true;
}
