namespace DataAccessLayer.Entities;

public class TestQuestion : CategoryLikeEntity
{
    public string Question { get; set; } = string.Empty;
    public string GroundTruth { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
}
