namespace Domain.Entities;

public class TestQuestion : CategoryLikeEntity
{
    public int SubjectId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string? Difficulty { get; set; }

    public virtual Subject Subject { get; set; } = null!;
    public virtual ICollection<TestResponse> TestResponses { get; } = [];
}
