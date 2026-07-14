using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Pages.Admin;
using System.Reflection;

namespace UnitTests;

[TestFixture]
public class AdminAiConfigurationPageTests
{
    private Mock<IAiConfigurationAdminService> _serviceMock = null!;
    private AiConfigurationModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IAiConfigurationAdminService>();
        _sut = new AiConfigurationModel(_serviceMock.Object);
    }

    [Test]
    public void PageModel_HasAdminAuthorizeAttribute()
    {
        // Act
        var attribute = typeof(AiConfigurationModel).GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.Roles, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task OnGetSubjectsAsync_CallsServiceAndReturnsJson()
    {
        // Arrange
        var subjects = new List<AiConfigurationSubjectOptionDto>
        {
            new() { SubjectId = 1, SubjectCode = "DB201", SubjectName = "Database" }
        };
        _serviceMock.Setup(s => s.GetSubjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjects);

        // Act
        var result = await _sut.OnGetSubjectsAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.EqualTo(subjects));
        _serviceMock.Verify(s => s.GetSubjectsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnGetOptionsAsync_CallsServiceAndReturnsJson()
    {
        // Arrange
        var options = new AiConfigurationOptionsDto
        {
            ChunkingStrategies = [],
            EmbeddingModels = [],
            ChatModels = [],
            JudgeModels = []
        };
        _serviceMock.Setup(s => s.GetOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(options);

        // Act
        var result = await _sut.OnGetOptionsAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.EqualTo(options));
    }

    [Test]
    public async Task OnGetConfigurationAsync_WhenFound_ReturnsJson()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnGetConfigurationAsync(3, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.EqualTo(mockDto));
    }

    [Test]
    public async Task OnGetConfigurationAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectAiConfigurationDto?)null);

        // Act
        var result = await _sut.OnGetConfigurationAsync(99, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task OnPutConfigurationAsync_WithValidData_SavesAndReturnsJson()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        var request = new SaveSubjectAiConfigurationRequest
        {
            ChunkSize = 1200,
            ChunkOverlap = 180,
            LlmModel = "gemini-3.5-flash"
        };
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);
        _serviceMock.Setup(s => s.SaveSubjectConfigurationAsync(3, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnPutConfigurationAsync(3, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        Assert.That(jsonResult.Value, Is.EqualTo(mockDto));
        _serviceMock.Verify(s => s.SaveSubjectConfigurationAsync(3, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnPutConfigurationAsync_WithInvalidChunkSize_ReturnsBadRequest()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        var request = new SaveSubjectAiConfigurationRequest
        {
            ChunkSize = 99 // Invalid: must be 100..8000
        };
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnPutConfigurationAsync(3, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        _serviceMock.Verify(s => s.SaveSubjectConfigurationAsync(It.IsAny<int>(), It.IsAny<SaveSubjectAiConfigurationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnPutConfigurationAsync_WithOverlapEqualsOrExceedsChunkSize_ReturnsBadRequest()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        var request = new SaveSubjectAiConfigurationRequest
        {
            ChunkSize = 1000,
            ChunkOverlap = 1000 // Invalid: overlap must be < chunkSize
        };
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnPutConfigurationAsync(3, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task OnPutConfigurationAsync_WithNegativeTopK_ReturnsBadRequest()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        var request = new SaveSubjectAiConfigurationRequest
        {
            TopK = -5 // Invalid: must be positive
        };
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnPutConfigurationAsync(3, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task OnPutConfigurationAsync_WithInvalidSimilarity_ReturnsBadRequest()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        var request = new SaveSubjectAiConfigurationRequest
        {
            SimilarityThreshold = 1.05 // Invalid: must be 0..1
        };
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnPutConfigurationAsync(3, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task OnPutConfigurationAsync_WithEmptyModelStrings_ReturnsBadRequest()
    {
        // Arrange
        var mockDto = CreateMockSubjectConfigDto();
        var request = new SaveSubjectAiConfigurationRequest
        {
            LlmModel = "   " // Invalid: cannot be empty or whitespace
        };
        _serviceMock.Setup(s => s.GetSubjectConfigurationAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _sut.OnPutConfigurationAsync(3, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task OnPostReindexAsync_OnSuccess_Returns202Accepted()
    {
        // Arrange
        var response = new SubjectReindexResponseDto
        {
            SubjectId = 3,
            QueuedDocumentCount = 5,
            QueuedAt = System.DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.ReindexSubjectAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.OnPostReindexAsync(3, CancellationToken.None);

        // Assert
        Assert.That(result, Is.TypeOf<ObjectResult>());
        var objResult = (ObjectResult)result;
        Assert.That(objResult.StatusCode, Is.EqualTo(202));
        Assert.That(objResult.Value, Is.EqualTo(response));
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
