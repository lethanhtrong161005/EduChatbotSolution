namespace Domain.Entities;

public class TestResponseContext : NaturalEntity
{
    public Guid TestResponseId { get; set; }
    public int RetrievalRank { get; set; }
    public int? PromptOrder { get; set; }
    public bool WasIncludedInPrompt { get; set; }
    public Guid? ChunkId { get; set; }
    public Guid? SourceChunkId { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public int? SourceSubjectId { get; set; }
    public int? ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public double? SimilarityScore { get; set; }
    public string? DocumentTitle { get; set; }
    public string? DocumentFileName { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public int? StartPageNumber { get; set; }
    public int? EndPageNumber { get; set; }
    public string? StartSectionTitle { get; set; }
    public string? EndSectionTitle { get; set; }

    public virtual TestResponse TestResponse { get; set; } = null!;
    public virtual Chunk? Chunk { get; set; }
}
