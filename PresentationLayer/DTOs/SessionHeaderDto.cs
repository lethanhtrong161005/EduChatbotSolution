namespace Presentation.DTOs;

public class SessionHeaderDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; }
}
