namespace DataAccessLayer.Entities;

public class TestResponse : NaturalEntity
{
    public Guid ExperimentId { get; set; }
    public int TestQuestionId { get; set; }
    public string GeneratedAnswer { get; set; } = string.Empty;
    public double RagasContextPrecision { get; set; }
    public double RagasContextRecall { get; set; }
    public double RagasFaithfulness { get; set; }
    public double RagasResponseRelevancy { get; set; }
    public int LatencyTimeToFirstTokenMs { get; set; }
    public int LatencyTimePerOutputTokenMs { get; set; }
    public int LatencyTotalGenerationTimeMs { get; set; }

    public virtual Experiment Experiment { get; set; } = null!;
    public virtual TestQuestion TestQuestion { get; set; } = null!;
}
