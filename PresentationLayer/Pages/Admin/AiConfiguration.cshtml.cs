using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Presentation.Pages.Admin;

/// <summary>
/// PageModel for the Subject AI Configuration admin page.
/// Handles retrieval, updating, and reindexing of AI settings for subjects.
/// </summary>
[Authorize(Roles = "Admin")]
public class AiConfigurationModel(IAiConfigurationAdminService adminService) : PageModel
{
    private readonly IAiConfigurationAdminService _adminService = adminService;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetSubjectsAsync(CancellationToken cxlTkn)
    {
        try
        {
            var subjects = await _adminService.GetSubjectsAsync(cxlTkn);
            return new JsonResult(subjects);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    public async Task<IActionResult> OnGetOptionsAsync(CancellationToken cxlTkn)
    {
        try
        {
            var options = await _adminService.GetOptionsAsync(cxlTkn);
            return new JsonResult(options);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    public async Task<IActionResult> OnGetConfigurationAsync([FromQuery] int subjectId, CancellationToken cxlTkn)
    {
        try
        {
            var config = await _adminService.GetSubjectConfigurationAsync(subjectId, cxlTkn);
            if (config == null)
            {
                return NotFound(new { error = $"AI configuration for Subject {subjectId} not found." });
            }
            return new JsonResult(config);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    public async Task<IActionResult> OnPutConfigurationAsync([FromQuery] int subjectId, [FromBody] SaveSubjectAiConfigurationRequest request, CancellationToken cxlTkn)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body cannot be null." });
        }

        // Fetch current configuration to resolve effective ChunkSize for overlap validation
        SubjectAiConfigurationDto? currentConfig;
        try
        {
            currentConfig = await _adminService.GetSubjectConfigurationAsync(subjectId, cxlTkn);
            if (currentConfig == null)
            {
                return NotFound(new { error = $"Subject with ID {subjectId} not found." });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        // Perform server-side validation
        if (request.ChunkSize.HasValue && (request.ChunkSize.Value < 100 || request.ChunkSize.Value > 8000))
        {
            ModelState.AddModelError(nameof(request.ChunkSize), "ChunkSize must be between 100 and 8000.");
        }

        int effectiveChunkSize = request.ChunkSize ?? currentConfig.Indexing.ChunkSize.GlobalDefault;

        if (request.ChunkOverlap.HasValue)
        {
            if (request.ChunkOverlap.Value < 0)
            {
                ModelState.AddModelError(nameof(request.ChunkOverlap), "ChunkOverlap must be non-negative.");
            }
            if (request.ChunkOverlap.Value >= effectiveChunkSize)
            {
                ModelState.AddModelError(nameof(request.ChunkOverlap), $"ChunkOverlap must be less than effective ChunkSize ({effectiveChunkSize}).");
            }
        }

        if (request.TopK.HasValue && request.TopK.Value <= 0)
        {
            ModelState.AddModelError(nameof(request.TopK), "TopK must be positive.");
        }

        if (request.SimilarityThreshold.HasValue && (request.SimilarityThreshold.Value < 0.0 || request.SimilarityThreshold.Value > 1.0))
        {
            ModelState.AddModelError(nameof(request.SimilarityThreshold), "SimilarityThreshold must be between 0 and 1.");
        }

        if (request.MaxContextChunks.HasValue && request.MaxContextChunks.Value <= 0)
        {
            ModelState.AddModelError(nameof(request.MaxContextChunks), "MaxContextChunks must be positive.");
        }

        if (request.MaxHistoryMessages.HasValue && request.MaxHistoryMessages.Value < 0)
        {
            ModelState.AddModelError(nameof(request.MaxHistoryMessages), "MaxHistoryMessages must be non-negative.");
        }

        if (request.ChatTemperature.HasValue && (request.ChatTemperature.Value < 0f || request.ChatTemperature.Value > 2f))
        {
            ModelState.AddModelError(nameof(request.ChatTemperature), "ChatTemperature must be between 0 and 2.");
        }

        if (request.TitleTemperature.HasValue && (request.TitleTemperature.Value < 0f || request.TitleTemperature.Value > 2f))
        {
            ModelState.AddModelError(nameof(request.TitleTemperature), "TitleTemperature must be between 0 and 2.");
        }

        if (request.CitationExtractionTemperature.HasValue && (request.CitationExtractionTemperature.Value < 0f || request.CitationExtractionTemperature.Value > 2f))
        {
            ModelState.AddModelError(nameof(request.CitationExtractionTemperature), "CitationExtractionTemperature must be between 0 and 2.");
        }

        // Empty/whitespace strings are invalid for names, strategies, and prompts
        if (request.ChunkingStrategy != null && string.IsNullOrWhiteSpace(request.ChunkingStrategy))
            ModelState.AddModelError(nameof(request.ChunkingStrategy), "ChunkingStrategy cannot be empty or whitespace.");
        if (request.EmbeddingModel != null && string.IsNullOrWhiteSpace(request.EmbeddingModel))
            ModelState.AddModelError(nameof(request.EmbeddingModel), "EmbeddingModel cannot be empty or whitespace.");
        if (request.LlmModel != null && string.IsNullOrWhiteSpace(request.LlmModel))
            ModelState.AddModelError(nameof(request.LlmModel), "LlmModel cannot be empty or whitespace.");
        if (request.ChatPrompt != null && string.IsNullOrWhiteSpace(request.ChatPrompt))
            ModelState.AddModelError(nameof(request.ChatPrompt), "ChatPrompt cannot be empty or whitespace.");
        if (request.ContextPrompt != null && string.IsNullOrWhiteSpace(request.ContextPrompt))
            ModelState.AddModelError(nameof(request.ContextPrompt), "ContextPrompt cannot be empty or whitespace.");
        if (request.NoContextRetrievedPrompt != null && string.IsNullOrWhiteSpace(request.NoContextRetrievedPrompt))
            ModelState.AddModelError(nameof(request.NoContextRetrievedPrompt), "NoContextRetrievedPrompt cannot be empty or whitespace.");
        if (request.TitlePrompt != null && string.IsNullOrWhiteSpace(request.TitlePrompt))
            ModelState.AddModelError(nameof(request.TitlePrompt), "TitlePrompt cannot be empty or whitespace.");
        if (request.CitationExtractionPrompt != null && string.IsNullOrWhiteSpace(request.CitationExtractionPrompt))
            ModelState.AddModelError(nameof(request.CitationExtractionPrompt), "CitationExtractionPrompt cannot be empty or whitespace.");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updatedConfig = await _adminService.SaveSubjectConfigurationAsync(subjectId, request, cxlTkn);
            return new JsonResult(updatedConfig);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    public async Task<IActionResult> OnPostReindexAsync([FromQuery] int subjectId, CancellationToken cxlTkn)
    {
        try
        {
            var response = await _adminService.ReindexSubjectAsync(subjectId, cxlTkn);
            return StatusCode(202, response);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }
}
