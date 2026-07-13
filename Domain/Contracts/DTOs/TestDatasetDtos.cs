namespace Domain.Contracts.DTOs;

public record TestDatasetDto
{
    public required string DatasetKey { get; init; }
    public required string Language { get; init; }
    public required string SubjectCode { get; init; }
    public required IReadOnlyList<TestDatasetQuestionDto> Questions { get; init; }
}

public record TestDatasetQuestionDto
{
    public required string ExternalId { get; init; }
    public required string Question { get; init; }
    public required string GroundTruth { get; init; }
}
