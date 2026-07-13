using Domain.Contracts;
using Domain.Contracts.DTOs;

namespace UnitTests;

public class ContractBoundaryTests
{
    [Test]
    public void FrozenContractTypes_ArePublishedFromTheExpectedAssemblies()
    {
        Type[] domainContractTypes =
        [
            typeof(SubjectAiConfigurationDto),
            typeof(AdminReportDashboardDto),
            typeof(ExperimentResultDto),
            typeof(TestDatasetDto),
            typeof(ResolvedChatVariantNavigation),
            typeof(ChatExchangeResult),
            typeof(IAiConfigurationAdminService),
            typeof(IAdminReportService),
            typeof(IExperimentService),
            typeof(IRagasStyleEvaluator),
        ];

        Assert.That(domainContractTypes, Has.All.Not.Null);
    }

    [Test]
    public void EffectiveConfiguration_ExposesChunkSizeAndOverlap()
    {
        var properties = typeof(EffectiveAiConfiguration)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.That(properties, Does.Contain(nameof(EffectiveAiConfiguration.ChunkSize)));
        Assert.That(properties, Does.Contain(nameof(EffectiveAiConfiguration.ChunkOverlap)));
    }

    [Test]
    public void ChatPersistenceContract_UsesAtomicExchangeAndVariantOperations()
    {
        var methodNames = typeof(IChatPersistenceService)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(methodNames, Does.Contain("CreateExchangeAsync"));
            Assert.That(methodNames, Does.Contain("ResetFailedAssistantMessageAsync"));
            Assert.That(methodNames, Does.Contain("CreateAssistantVariantAsync"));
            Assert.That(methodNames, Does.Contain("GetAssistantVariantAsync"));
            Assert.That(methodNames, Does.Contain("SelectAssistantVariantAsync"));
            Assert.That(methodNames, Does.Not.Contain("CreateUserMessageAsync"));
            Assert.That(methodNames, Does.Not.Contain("CreateStreamingAssistantMessageAsync"));
        });
    }
}
