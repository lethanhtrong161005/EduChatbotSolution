using System;
using System.Reflection;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Presentation.Pages.Admin.Experiments;

namespace UnitTests;

[TestFixture]
public class AdminExperimentCreatePageTests
{
    private Mock<IExperimentService> _serviceMock = null!;
    private CreateModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IExperimentService>();
        _sut = new CreateModel(_serviceMock.Object);
    }

    [Test]
    public void PageModel_HasAdminAuthorizeAttribute()
    {
        // Act
        var attribute = typeof(CreateModel).GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.Roles, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task OnGetOptionsAsync_WhenFound_ReturnsJson()
    {
        // Arrange
        var mockOptions = new ExperimentCreateOptionsDto
        {
            SubjectId = 3,
            SubjectCode = "DB201",
            SubjectName = "Database",
            CurrentConfiguration = CreateMockSubjectConfigDto(),
            AiOptions = new AiConfigurationOptionsDto { ChunkingStrategies = [], EmbeddingModels = [], ChatModels = [], JudgeModels = [] },
            TestQuestions = []
        };
        _serviceMock.Setup(s => s.GetCreateOptionsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOptions);

        // Act
        var result = await _sut.OnGetOptionsAsync(3, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.EqualTo(mockOptions));
    }

    [Test]
    public async Task OnGetOptionsAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetCreateOptionsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExperimentCreateOptionsDto?)null);

        // Act
        var result = await _sut.OnGetOptionsAsync(99, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task OnPostPreflightAsync_WithValidRequest_ReturnsPreflightResult()
    {
        // Arrange
        var request = new ExperimentIndexPreflightRequest
        {
            SubjectId = 3,
            ChunkingStrategy = "FixedLength",
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = "model1"
        };
        var response = new ExperimentIndexPreflightDto
        {
            IsCompatible = true,
            RequiresReindex = false,
            AffectedDocumentCount = 0,
            BlockingReason = null
        };
        _serviceMock.Setup(s => s.PreflightAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.OnPostPreflightAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task OnPostPreflightAsync_WithInvalidChunkSize_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExperimentIndexPreflightRequest
        {
            SubjectId = 3,
            ChunkingStrategy = "FixedLength",
            ChunkSize = 99, // Invalid
            ChunkOverlap = 200,
            EmbeddingModel = "model1"
        };

        // Act
        var result = await _sut.OnPostPreflightAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        _serviceMock.Verify(s => s.PreflightAsync(It.IsAny<ExperimentIndexPreflightRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnPostExperimentAsync_WithValidRequest_Returns202Accepted()
    {
        // Arrange
        var request = new CreateExperimentRequest
        {
            ExperimentName = "Test Run",
            SubjectId = 3,
            TestQuestionIds = [1, 2, 3],
            ChunkingStrategy = "FixedLength",
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = "model1",
            TopK = 15,
            SimilarityThreshold = 0.6,
            MaxContextChunks = 8,
            LlmModel = "llm1",
            ChatTemperature = 0.3f,
            JudgeModel = "judge1"
        };
        var response = new CreateExperimentResponse
        {
            ExperimentId = Guid.NewGuid(),
            Status = ExperimentStatus.Queued,
            QueuedAt = DateTime.UtcNow,
            QuestionCount = 3,
            RequiresReindex = false,
            AffectedDocumentCount = 0
        };
        _serviceMock.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.OnPostExperimentAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<ObjectResult>());
        var objResult = (ObjectResult)result;
        Assert.That(objResult.StatusCode, Is.EqualTo(202));
        Assert.That(objResult.Value, Is.EqualTo(response));
        _serviceMock.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnPostExperimentAsync_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateExperimentRequest
        {
            ExperimentName = "  ", // Invalid
            SubjectId = 3,
            TestQuestionIds = [1],
            ChunkingStrategy = "FixedLength",
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = "model1",
            TopK = 15,
            SimilarityThreshold = 0.6,
            MaxContextChunks = 8,
            LlmModel = "llm1",
            ChatTemperature = 0.3f,
            JudgeModel = "judge1"
        };

        // Act
        var result = await _sut.OnPostExperimentAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreateExperimentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnPostExperimentAsync_WithNoQuestions_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateExperimentRequest
        {
            ExperimentName = "Test Run",
            SubjectId = 3,
            TestQuestionIds = [], // Invalid
            ChunkingStrategy = "FixedLength",
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = "model1",
            TopK = 15,
            SimilarityThreshold = 0.6,
            MaxContextChunks = 8,
            LlmModel = "llm1",
            ChatTemperature = 0.3f,
            JudgeModel = "judge1"
        };

        // Act
        var result = await _sut.OnPostExperimentAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task OnPostExperimentAsync_WithDuplicateQuestionIds_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateExperimentRequest
        {
            ExperimentName = "Test Run",
            SubjectId = 3,
            TestQuestionIds = [1, 2, 1], // Invalid: duplicate 1
            ChunkingStrategy = "FixedLength",
            ChunkSize = 1000,
            ChunkOverlap = 200,
            EmbeddingModel = "model1",
            TopK = 15,
            SimilarityThreshold = 0.6,
            MaxContextChunks = 8,
            LlmModel = "llm1",
            ChatTemperature = 0.3f,
            JudgeModel = "judge1"
        };

        // Act
        var result = await _sut.OnPostExperimentAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    private static SubjectAiConfigurationDto CreateMockSubjectConfigDto()
    {
        return new SubjectAiConfigurationDto
        {
            SubjectId = 3,
            SubjectCode = "DB201",
            SubjectName = "Database",
            Indexing = new AiIndexingConfigurationDto
            {
                ChunkingStrategy = new AiStringSettingDto { GlobalDefault = "FixedLength", StoredOverride = null, EffectiveValue = "FixedLength" },
                ChunkSize = new AiIntSettingDto { GlobalDefault = 1000, StoredOverride = null, EffectiveValue = 1000 },
                ChunkOverlap = new AiIntSettingDto { GlobalDefault = 200, StoredOverride = null, EffectiveValue = 200 },
                EmbeddingModel = new AiStringSettingDto { GlobalDefault = "model1", StoredOverride = null, EffectiveValue = "model1" }
            },
            Retrieval = new AiRetrievalConfigurationDto
            {
                TopK = new AiIntSettingDto { GlobalDefault = 15, StoredOverride = null, EffectiveValue = 15 },
                SimilarityThreshold = new AiDoubleSettingDto { GlobalDefault = 0.6, StoredOverride = null, EffectiveValue = 0.6 },
                MaxContextChunks = new AiIntSettingDto { GlobalDefault = 8, StoredOverride = null, EffectiveValue = 8 }
            },
            Generation = new AiGenerationConfigurationDto
            {
                LlmModel = new AiStringSettingDto { GlobalDefault = "llm1", StoredOverride = null, EffectiveValue = "llm1" },
                ChatTemperature = new AiFloatSettingDto { GlobalDefault = 0.3f, StoredOverride = null, EffectiveValue = 0.3f },
                MaxHistoryMessages = new AiIntSettingDto { GlobalDefault = 12, StoredOverride = null, EffectiveValue = 12 },
                TitleTemperature = new AiFloatSettingDto { GlobalDefault = 0.0f, StoredOverride = null, EffectiveValue = 0.0f },
                CitationExtractionTemperature = new AiFloatSettingDto { GlobalDefault = 0.0f, StoredOverride = null, EffectiveValue = 0.0f }
            },
            Prompts = new AiPromptConfigurationDto
            {
                ChatPrompt = new AiStringSettingDto { GlobalDefault = "prompt1", StoredOverride = null, EffectiveValue = "prompt1" },
                ContextPrompt = new AiStringSettingDto { GlobalDefault = "prompt2", StoredOverride = null, EffectiveValue = "prompt2" },
                NoContextRetrievedPrompt = new AiStringSettingDto { GlobalDefault = "prompt3", StoredOverride = null, EffectiveValue = "prompt3" },
                TitlePrompt = new AiStringSettingDto { GlobalDefault = "prompt4", StoredOverride = null, EffectiveValue = "prompt4" },
                CitationExtractionPrompt = new AiStringSettingDto { GlobalDefault = "prompt5", StoredOverride = null, EffectiveValue = "prompt5" }
            }
        };
    }
}
