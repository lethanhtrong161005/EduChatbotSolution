using Domain.Contracts.DTOs;
using Presentation.DTOs;
using System.Text.Json;

namespace UnitTests;

[NUnit.Framework.Ignore("Requires local git-ignored .agents folder containing contract fixtures")]
public class ContractFixtureSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEnumerable<TestCaseData> Fixtures()
    {
        yield return Case("subject-ai-configuration.json", typeof(SubjectAiConfigurationDto));
        yield return Case("ai-configuration-options.json", typeof(AiConfigurationOptionsDto));
        yield return Case("subject-reindex-response.json", typeof(SubjectReindexResponseDto));
        yield return Case("admin-report-dashboard.json", typeof(AdminReportDashboardDto));
        yield return Case("experiment-create-options.json", typeof(ExperimentCreateOptionsDto));
        yield return Case("experiment-index-preflight-request.json", typeof(ExperimentIndexPreflightRequest));
        yield return Case("experiment-index-preflight.json", typeof(ExperimentIndexPreflightDto));
        yield return Case("experiment-create-request.json", typeof(CreateExperimentRequest));
        yield return Case("experiment-create-response.json", typeof(CreateExperimentResponse));
        yield return Case("experiment-summaries.json", typeof(List<ExperimentSummaryDto>));
        yield return Case("experiment-result.json", typeof(ExperimentResultDto));
        yield return Case("experiment-result-preparing-index.json", typeof(ExperimentResultDto));
        yield return Case("experiment-comparison.json", typeof(ExperimentComparisonDto));
        yield return Case("chat-session-variants.json", typeof(ChatSessionDto));
        yield return Case("chat-variant-response.json", typeof(ChatMessageDto));
        yield return Case("chat-generation-start.json", typeof(StartAssistantGenerationResponse));
    }

    [TestCaseSource(nameof(Fixtures))]
    public void FrozenFixture_DeserializesToPublishedContract(string fileName, Type contractType)
    {
        var json = File.ReadAllText(Path.Combine(FixtureDirectory(), fileName));
        var result = JsonSerializer.Deserialize(json, contractType, JsonOptions);

        Assert.That(result, Is.Not.Null);
    }

    private static TestCaseData Case(string fileName, Type type)
        => new(fileName, type) { TestName = $"Fixture_{Path.GetFileNameWithoutExtension(fileName)}_Deserializes" };

    private static string FixtureDirectory()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            ".agents", "contracts", "fixtures"));
}
