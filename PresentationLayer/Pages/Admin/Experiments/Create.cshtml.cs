using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Presentation.Pages.Admin.Experiments;

/// <summary>
/// PageModel for creating RAG AI experiments/benchmarks.
/// Handles preflight compatibility checks and experiment creation.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel(IExperimentService experimentService) : PageModel
{
    private readonly IExperimentService _experimentService = experimentService;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetOptionsAsync([FromQuery] int subjectId, CancellationToken cxlTkn)
    {
        try
        {
            var options = await _experimentService.GetCreateOptionsAsync(subjectId, cxlTkn);
            if (options == null)
            {
                return NotFound(new { error = $"Options for subject {subjectId} not found." });
            }
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

    public async Task<IActionResult> OnPostPreflightAsync([FromBody] ExperimentIndexPreflightRequest request, CancellationToken cxlTkn)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body cannot be null." });
        }

        if (request.ChunkSize < 100 || request.ChunkSize > 8000)
        {
            ModelState.AddModelError(nameof(request.ChunkSize), "ChunkSize must be between 100 and 8000.");
        }

        if (request.ChunkOverlap < 0 || request.ChunkOverlap >= request.ChunkSize)
        {
            ModelState.AddModelError(nameof(request.ChunkOverlap), "ChunkOverlap must be non-negative and less than ChunkSize.");
        }

        if (string.IsNullOrWhiteSpace(request.ChunkingStrategy))
        {
            ModelState.AddModelError(nameof(request.ChunkingStrategy), "ChunkingStrategy is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EmbeddingModel))
        {
            ModelState.AddModelError(nameof(request.EmbeddingModel), "EmbeddingModel is required.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _experimentService.PreflightAsync(request, cxlTkn);
            if (result == null)
            {
                return NotFound(new { error = "Preflight check failed: subject not found." });
            }
            return new JsonResult(result);
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

    public async Task<IActionResult> OnPostExperimentAsync([FromBody] CreateExperimentRequest request, CancellationToken cxlTkn)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body cannot be null." });
        }

        // Perform server-side validation
        if (string.IsNullOrWhiteSpace(request.ExperimentName))
        {
            ModelState.AddModelError(nameof(request.ExperimentName), "ExperimentName is required.");
        }

        if (request.SubjectId <= 0)
        {
            ModelState.AddModelError(nameof(request.SubjectId), "SubjectId must be positive.");
        }

        if (request.TestQuestionIds == null || request.TestQuestionIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.TestQuestionIds), "At least one test question must be selected.");
        }
        else
        {
            var distinctCount = request.TestQuestionIds.Distinct().Count();
            if (distinctCount != request.TestQuestionIds.Count)
            {
                ModelState.AddModelError(nameof(request.TestQuestionIds), "Duplicate question IDs are not allowed.");
            }
            if (distinctCount > 50)
            {
                ModelState.AddModelError(nameof(request.TestQuestionIds), "Cannot select more than 50 questions.");
            }
        }

        if (request.ChunkSize < 100 || request.ChunkSize > 8000)
        {
            ModelState.AddModelError(nameof(request.ChunkSize), "ChunkSize must be between 100 and 8000.");
        }

        if (request.ChunkOverlap < 0 || request.ChunkOverlap >= request.ChunkSize)
        {
            ModelState.AddModelError(nameof(request.ChunkOverlap), "ChunkOverlap must be non-negative and less than ChunkSize.");
        }

        if (request.TopK <= 0)
        {
            ModelState.AddModelError(nameof(request.TopK), "TopK must be positive.");
        }

        if (request.SimilarityThreshold < 0.0 || request.SimilarityThreshold > 1.0)
        {
            ModelState.AddModelError(nameof(request.SimilarityThreshold), "SimilarityThreshold must be between 0 and 1.");
        }

        if (request.MaxContextChunks <= 0)
        {
            ModelState.AddModelError(nameof(request.MaxContextChunks), "MaxContextChunks must be positive.");
        }

        if (request.ChatTemperature < 0f || request.ChatTemperature > 2f)
        {
            ModelState.AddModelError(nameof(request.ChatTemperature), "ChatTemperature must be between 0 and 2.");
        }

        if (string.IsNullOrWhiteSpace(request.ChunkingStrategy))
            ModelState.AddModelError(nameof(request.ChunkingStrategy), "ChunkingStrategy is required.");
        if (string.IsNullOrWhiteSpace(request.EmbeddingModel))
            ModelState.AddModelError(nameof(request.EmbeddingModel), "EmbeddingModel is required.");
        if (string.IsNullOrWhiteSpace(request.LlmModel))
            ModelState.AddModelError(nameof(request.LlmModel), "LlmModel is required.");
        if (string.IsNullOrWhiteSpace(request.EvaluatorLlmProvider)) ModelState.AddModelError(nameof(request.EvaluatorLlmProvider), "EvaluatorLlmProvider is required.");
        if (string.IsNullOrWhiteSpace(request.EvaluatorLlmModel)) ModelState.AddModelError(nameof(request.EvaluatorLlmModel), "EvaluatorLlmModel is required.");
        if (string.IsNullOrWhiteSpace(request.EvaluatorEmbeddingProvider)) ModelState.AddModelError(nameof(request.EvaluatorEmbeddingProvider), "EvaluatorEmbeddingProvider is required.");
        if (string.IsNullOrWhiteSpace(request.EvaluatorEmbeddingModel)) ModelState.AddModelError(nameof(request.EvaluatorEmbeddingModel), "EvaluatorEmbeddingModel is required.");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _experimentService.CreateAsync(request, cxlTkn);
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
