using AutoMapper;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Business.Services.AI.Experiments;

public sealed class ExperimentService(
    IUnitOfWork unitOfWork,
    IAiConfigurationResolver configurationResolver,
    IAiConfigurationAdminService configurationAdminService,
    IExperimentDatasetProvider datasetProvider,
    IExperimentDispatcher dispatcher,
    IPythonRagasClient ragasClient,
    IMapper mapper)
    : IExperimentService
{
    private const string EvaluatorMetricSetKey = "ragas-rag-core-v1";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiConfigurationResolver _configurationResolver = configurationResolver;
    private readonly IAiConfigurationAdminService _configurationAdminService = configurationAdminService;
    private readonly IExperimentDatasetProvider _datasetProvider = datasetProvider;
    private readonly IExperimentDispatcher _dispatcher = dispatcher;
    private readonly IPythonRagasClient _ragasClient = ragasClient;
    private readonly IMapper _mapper = mapper;

    public async Task<ExperimentCreateOptionsDto?> GetCreateOptionsAsync(int subjectId, CancellationToken cxlTkn = default)
    {
        if (subjectId <= 0) throw new BadRequestException("Subject ID must be positive.");
        await _datasetProvider.ImportAsync(cxlTkn);

        var subject = await _unitOfWork.Subjects.FindByIdAsync(subjectId, cxlTkn);
        if (subject == null) return null;

        var configuration = await _configurationAdminService.GetSubjectConfigurationAsync(subjectId, cxlTkn);
        if (configuration == null) return null;

        var options = await _configurationAdminService.GetOptionsAsync(cxlTkn);
        var evaluatorCapabilities = await _ragasClient.GetCapabilitiesAsync(cxlTkn);

        var questions = await _unitOfWork.TestQuestions.GetAsync(
            preFilter: e => e.SubjectId == subjectId,
            projection: e => new TestQuestionOptionDto { TestQuestionId = e.Id, ExternalId = e.ExternalId, Question = e.Question },
            orderBy: q => q.OrderBy(e => e.ExternalId),
            asNoTracking: true,
            cancellationToken: cxlTkn);

        return new ExperimentCreateOptionsDto
        {
            SubjectId = subject.Id,
            SubjectCode = subject.Code,
            SubjectName = subject.Name,
            CurrentConfiguration = configuration,
            AiOptions = options,
            TestQuestions = [.. questions],
            EvaluatorCapabilities = evaluatorCapabilities,
        };
    }

    public async Task<ExperimentIndexPreflightDto?> PreflightAsync(ExperimentIndexPreflightRequest request, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIndexing(request.SubjectId, request.ChunkingStrategy, request.ChunkSize, request.ChunkOverlap, request.EmbeddingModel);

        var subject = await _unitOfWork.Subjects.FindByIdAsync(request.SubjectId, cxlTkn);
        if (subject == null) return null;

        if (subject.IndexAvailability == SubjectIndexAvailability.Reindexing)
            return new ExperimentIndexPreflightDto { IsCompatible = false, RequiresReindex = true, AffectedDocumentCount = 0, BlockingReason = "The subject is currently being reindexed." };

        var activeRun = await _unitOfWork.Experiments.ExistsAsync(e => e.SubjectId == request.SubjectId && e.Status != ExperimentStatus.Completed && e.Status != ExperimentStatus.Failed, cxlTkn);
        if (activeRun)
            return new ExperimentIndexPreflightDto { IsCompatible = false, RequiresReindex = false, AffectedDocumentCount = 0, BlockingReason = "Another experiment is already active for this subject." };

        var affected = await _unitOfWork.ExperimentRuns.CountAffectedDocumentsAsync(request.SubjectId, request.ChunkingStrategy, request.ChunkSize, request.ChunkOverlap, request.EmbeddingModel, cxlTkn);
        return new ExperimentIndexPreflightDto { IsCompatible = affected == 0, RequiresReindex = affected > 0, AffectedDocumentCount = affected };
    }

    public async Task<CreateExperimentResponse> CreateAsync(CreateExperimentRequest request, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCreateRequest(request);

        var subject = await _unitOfWork.Subjects.FindByIdAsync(request.SubjectId, cxlTkn) ?? throw new EntityNotFoundException(request.SubjectId);
        if (subject.Code != "DB201") throw new EntityValidationException("Demo experiments are restricted to the DB201 subject.", nameof(request.SubjectId));
        if (subject.IndexAvailability == SubjectIndexAvailability.Reindexing) throw new EntityConflictException("The subject is currently being reindexed.", nameof(Subject.IndexAvailability));

        await _datasetProvider.ImportAsync(cxlTkn);
        var dataset = await _datasetProvider.GetDatasetAsync(cxlTkn);

        var requestedIds = request.TestQuestionIds.Distinct().Order().ToArray();
        var questions = (await _unitOfWork.TestQuestions.GetAsync(
            filter: e => e.SubjectId == request.SubjectId && requestedIds.Contains(e.Id),
            orderBy: q => q.OrderBy(e => e.ExternalId),
            asNoTracking: true,
            cancellationToken: cxlTkn)).ToList();

        if (questions.Count != requestedIds.Length) throw new EntityValidationException("One or more selected questions do not belong to the requested subject.", nameof(request.TestQuestionIds));

        var current = await _configurationResolver.GetAiConfigurationAsync(request.SubjectId, cxlTkn);
        var evaluatorCapabilities = await _ragasClient.GetCapabilitiesAsync(cxlTkn);
        ValidateEvaluator(request, evaluatorCapabilities);
        var affected = await _unitOfWork.ExperimentRuns.CountAffectedDocumentsAsync(request.SubjectId, request.ChunkingStrategy, request.ChunkSize, request.ChunkOverlap, request.EmbeddingModel, cxlTkn);
        var experiment = new Experiment
        {
            ExperimentName = request.ExperimentName.Trim(),
            SubjectId = request.SubjectId,
            Status = ExperimentStatus.Queued,
            QuestionSetKey = CreateQuestionSetKey(dataset, questions),
            AffectedDocumentCount = affected,
            IndexedDocumentCount = 0,
            CompletedQuestionCount = 0,
            TotalQuestionCount = questions.Count,
            ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            Notes = request.Notes?.Trim(),
            ConfigurationSnapshot = new ExperimentConfigurationSnapshot
            {
                SubjectId = subject.Id,
                SubjectCode = subject.Code,
                SubjectName = subject.Name,
                ChunkingStrategy = request.ChunkingStrategy,
                ChunkSize = request.ChunkSize,
                ChunkOverlap = request.ChunkOverlap,
                EmbeddingProvider = AiProviderName.ForEmbeddingModel(request.EmbeddingModel),
                EmbeddingModel = request.EmbeddingModel,
                TopK = request.TopK,
                SimilarityThreshold = request.SimilarityThreshold,
                MaxContextChunks = request.MaxContextChunks,
                LlmProvider = AiProviderName.ForChatModel(request.LlmModel),
                LlmModel = request.LlmModel,
                ChatTemperature = request.ChatTemperature,
                MaxHistoryMessages = current.MaxHistoryMessages,
                ChatPrompt = current.ChatPrompt,
                ContextPrompt = current.ContextPrompt,
                NoContextRetrievedPrompt = current.NoContextRetrievedPrompt,
                CitationExtractionTemperature = current.CitationExtractionTemperature,
                CitationExtractionPrompt = current.CitationExtractionPrompt,
                EvaluatorLlmProvider = request.EvaluatorLlmProvider,
                EvaluatorLlmModel = request.EvaluatorLlmModel,
                EvaluatorEmbeddingProvider = request.EvaluatorEmbeddingProvider,
                EvaluatorEmbeddingModel = request.EvaluatorEmbeddingModel,
                EvaluatorMetricSetKey = EvaluatorMetricSetKey,
                EvaluatorPromptVersion = evaluatorCapabilities.PromptVersion,
            },
        };

        foreach (var question in questions) experiment.TestResponses.Add(new TestResponse
        {
            TestQuestionId = question.Id,
            SourceQuestionId = question.Id,
            DatasetName = dataset.DatasetName,
            DatasetKey = dataset.DatasetKey,
            DatasetVersion = dataset.DatasetVersion,
            QuestionExternalId = question.ExternalId,
            QuestionLanguage = question.Language,
            QuestionDifficulty = question.Difficulty,
            Question = question.Question,
            GroundTruth = question.GroundTruth,
            ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            Status = ExperimentQuestionStatus.Pending,
        });

        await _unitOfWork.ExperimentRuns.CreateQueuedAsync(experiment, cxlTkn);

        try
        {
            _dispatcher.Enqueue(experiment.Id);
        }
        catch (Exception ex)
        {
            experiment.Status = ExperimentStatus.Failed;
            experiment.FailureReason = $"The experiment could not be queued: {ex.Message}";
            experiment.CompletedAt = DateTime.UtcNow;
            _unitOfWork.Experiments.Update(experiment);
            await _unitOfWork.SaveAsync(CancellationToken.None);
            throw;
        }

        return new CreateExperimentResponse
        {
            ExperimentId = experiment.Id,
            Status = experiment.Status,
            QueuedAt = experiment.CreatedAt,
            QuestionCount = experiment.TotalQuestionCount,
            RequiresReindex = affected > 0,
            AffectedDocumentCount = affected,
        };
    }

    public async Task<IReadOnlyList<ExperimentSummaryDto>> GetSummariesAsync(CancellationToken cxlTkn = default)
    {
        var experiments = await _unitOfWork.Experiments.GetAsync(
            includeProperties: [nameof(Experiment.Subject), nameof(Experiment.ConfigurationSnapshot), nameof(Experiment.TestResponses), nameof(Experiment.TestResponses) + "." + nameof(TestResponse.CurrentEvaluationAttempt), nameof(Experiment.TestResponses) + "." + nameof(TestResponse.CurrentEvaluationAttempt) + "." + nameof(TestResponseEvaluationAttempt.Metrics)],
            orderBy: q => q.OrderByDescending(e => e.CreatedAt),
            asNoTracking: true,
            cancellationToken: cxlTkn);

        return [.. experiments.Select(_mapper.Map<ExperimentSummaryDto>)];
    }

    public async Task<ExperimentResultDto?> GetResultAsync(Guid experimentId, CancellationToken cxlTkn = default)
    {
        if (experimentId == Guid.Empty) throw new BadRequestException("Experiment ID cannot be empty.");

        var experiment = (await _unitOfWork.Experiments.GetAsync(
            includeProperties:
            [
                nameof(Experiment.Subject),
                nameof(Experiment.ConfigurationSnapshot),
                nameof(Experiment.TestResponses),
                nameof(Experiment.TestResponses) + "." + nameof(TestResponse.TestQuestion),
                nameof(Experiment.TestResponses) + "." + nameof(TestResponse.RetrievedContexts),
                nameof(Experiment.TestResponses) + "." + nameof(TestResponse.CurrentEvaluationAttempt),
                nameof(Experiment.TestResponses) + "." + nameof(TestResponse.CurrentEvaluationAttempt) + "." + nameof(TestResponseEvaluationAttempt.Metrics),
            ],
            filter: e => e.Id == experimentId,
            asNoTracking: true,
            asSplitQuery: true,
            cancellationToken: cxlTkn)).SingleOrDefault();

        if (experiment == null) return null;

        return new ExperimentResultDto
        {
            Summary = _mapper.Map<ExperimentSummaryDto>(experiment),
            Configuration = _mapper.Map<ExperimentConfigurationSnapshotDto>(experiment.ConfigurationSnapshot),
            Questions = [.. experiment.TestResponses.OrderBy(e => e.QuestionExternalId).Select(_mapper.Map<ExperimentQuestionResultDto>)],
        };
    }

    public async Task<ExperimentComparisonDto?> CompareAsync(Guid leftExperimentId, Guid rightExperimentId, CancellationToken cxlTkn = default)
    {
        if (leftExperimentId == Guid.Empty || rightExperimentId == Guid.Empty) throw new BadRequestException("Both experiment IDs are required.");
        if (leftExperimentId == rightExperimentId) throw new BadRequestException("An experiment cannot be compared with itself.");

        var left = await GetResultAsync(leftExperimentId, cxlTkn);
        var right = await GetResultAsync(rightExperimentId, cxlTkn);
        if (left == null || right == null) return null;

        if (left.Summary.Status != ExperimentStatus.Completed || right.Summary.Status != ExperimentStatus.Completed)
            throw new EntityConflictException("Only completed experiments can be compared.");
        if (left.Summary.SubjectId != right.Summary.SubjectId)
            throw new EntityConflictException("Compared experiments must belong to the same subject.");
        if (left.Summary.QuestionSetKey != right.Summary.QuestionSetKey)
            throw new EntityConflictException("Compared experiments must use the same question set.");
        if (left.Summary.ReconstructionCompleteness != ReconstructionCompleteness.Complete || right.Summary.ReconstructionCompleteness != ReconstructionCompleteness.Complete)
            throw new EntityConflictException("Legacy-incomplete experiments cannot be compared strictly.");
        if (string.IsNullOrWhiteSpace(left.Summary.EvaluatorProfileKey) || string.IsNullOrWhiteSpace(right.Summary.EvaluatorProfileKey) || left.Summary.EvaluatorProfileKey != right.Summary.EvaluatorProfileKey)
            throw new EntityConflictException("Compared experiments must use the same evaluator profile.");

        var rightQuestions = right.Questions.ToDictionary(e => e.ExternalId, StringComparer.Ordinal);
        return new ExperimentComparisonDto
        {
            Left = left.Summary,
            Right = right.Summary,
            Metrics =
            [
                Metric("Faithfulness", left.Summary.AggregateScores.Faithfulness, right.Summary.AggregateScores.Faithfulness, left.Summary.AggregateScores.Coverage.Faithfulness, right.Summary.AggregateScores.Coverage.Faithfulness),
                Metric("Answer relevancy", left.Summary.AggregateScores.AnswerRelevancy, right.Summary.AggregateScores.AnswerRelevancy, left.Summary.AggregateScores.Coverage.AnswerRelevancy, right.Summary.AggregateScores.Coverage.AnswerRelevancy),
                Metric("Context precision", left.Summary.AggregateScores.ContextPrecision, right.Summary.AggregateScores.ContextPrecision, left.Summary.AggregateScores.Coverage.ContextPrecision, right.Summary.AggregateScores.Coverage.ContextPrecision),
                Metric("Context recall", left.Summary.AggregateScores.ContextRecall, right.Summary.AggregateScores.ContextRecall, left.Summary.AggregateScores.Coverage.ContextRecall, right.Summary.AggregateScores.Coverage.ContextRecall),
            ],
            Questions =
            [
                .. left.Questions.OrderBy(e => e.ExternalId).Select(question =>
                {
                    var other = rightQuestions[question.ExternalId];
                    return new QuestionScoreComparisonDto { TestQuestionId = question.TestQuestionId, ExternalId = question.ExternalId, Question = question.Question, LeftScores = question.Scores, RightScores = other.Scores };
                }),
            ],
        };
    }

    private static void ValidateCreateRequest(CreateExperimentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExperimentName)) throw new EntityValidationException("Experiment name is required.", nameof(request.ExperimentName));
        if (request.ExperimentName.Trim().Length > 200) throw new EntityValidationException("Experiment name cannot exceed 200 characters.", nameof(request.ExperimentName));
        if (request.TestQuestionIds == null || request.TestQuestionIds.Count == 0) throw new EntityValidationException("At least one question must be selected.", nameof(request.TestQuestionIds));
        if (request.TestQuestionIds.Count > 50) throw new EntityValidationException("At most 50 questions may be selected.", nameof(request.TestQuestionIds));
        if (request.TestQuestionIds.Distinct().Count() != request.TestQuestionIds.Count) throw new EntityValidationException("Duplicate question IDs are not allowed.", nameof(request.TestQuestionIds));
        ValidateIndexing(request.SubjectId, request.ChunkingStrategy, request.ChunkSize, request.ChunkOverlap, request.EmbeddingModel);
        if (request.TopK <= 0) throw new EntityValidationException("Top K must be positive.", nameof(request.TopK));
        if (!double.IsFinite(request.SimilarityThreshold) || request.SimilarityThreshold is < 0 or > 1) throw new EntityValidationException("Similarity threshold must be between 0 and 1.", nameof(request.SimilarityThreshold));
        if (request.MaxContextChunks <= 0) throw new EntityValidationException("Maximum context chunks must be positive.", nameof(request.MaxContextChunks));
        if (string.IsNullOrWhiteSpace(request.LlmModel)) throw new EntityValidationException("LLM model is required.", nameof(request.LlmModel));
        if (!float.IsFinite(request.ChatTemperature) || request.ChatTemperature is < 0 or > 2) throw new EntityValidationException("Chat temperature must be between 0 and 2.", nameof(request.ChatTemperature));
        if (string.IsNullOrWhiteSpace(request.EvaluatorLlmProvider)) throw new EntityValidationException("Evaluator LLM provider is required.", nameof(request.EvaluatorLlmProvider));
        if (string.IsNullOrWhiteSpace(request.EvaluatorLlmModel)) throw new EntityValidationException("Evaluator LLM model is required.", nameof(request.EvaluatorLlmModel));
        if (string.IsNullOrWhiteSpace(request.EvaluatorEmbeddingProvider)) throw new EntityValidationException("Evaluator embedding provider is required.", nameof(request.EvaluatorEmbeddingProvider));
        if (string.IsNullOrWhiteSpace(request.EvaluatorEmbeddingModel)) throw new EntityValidationException("Evaluator embedding model is required.", nameof(request.EvaluatorEmbeddingModel));
    }

    private static void ValidateIndexing(int subjectId, string strategy, int size, int overlap, string embeddingModel)
    {
        if (subjectId <= 0) throw new EntityValidationException("Subject ID must be positive.", nameof(subjectId));
        if (strategy is not (ChunkingStrategy.FixedLength or ChunkingStrategy.RecursiveSeparator or ChunkingStrategy.SentenceParagraph)) throw new EntityValidationException($"Unknown chunking strategy '{strategy}'.", nameof(strategy));
        if (size is < 100 or > 8000) throw new EntityValidationException("Chunk size must be between 100 and 8000.", nameof(size));
        if (overlap < 0 || overlap >= size) throw new EntityValidationException("Chunk overlap must be non-negative and smaller than chunk size.", nameof(overlap));
        if (embeddingModel is not (EmbeddingModelName.BgeM3 or EmbeddingModelName.NemotronEmbedVLFree or EmbeddingModelName.GeminiEmbedding2)) throw new EntityValidationException($"Unknown embedding model '{embeddingModel}'.", nameof(embeddingModel));
    }

    private static string CreateQuestionSetKey(TestDatasetDto dataset, IEnumerable<TestQuestion> questions)
    {
        static string Field(string value) => $"{Encoding.UTF8.GetByteCount(value)}:{value}";
        var value = string.Join('|', [Field(dataset.DatasetName), Field(dataset.DatasetKey), Field(dataset.DatasetVersion), .. questions.OrderBy(e => e.ExternalId, StringComparer.Ordinal).SelectMany(e => new[] { Field(e.ExternalId), Field(e.Question), Field(e.GroundTruth) })]);
        return $"{dataset.DatasetKey}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()}";
    }

    private static void ValidateEvaluator(CreateExperimentRequest request, PythonRagasCapabilities capabilities)
    {
        if (capabilities.ContractVersion != ExperimentEvaluationService.ContractVersion) throw new EntityValidationException($"Unsupported evaluator contract '{capabilities.ContractVersion}'.", nameof(capabilities.ContractVersion));
        if (!RagasMetricName.All.All(capabilities.Metrics.Contains)) throw new EntityValidationException("The evaluator does not support the complete required metric set.", nameof(capabilities.Metrics));
        if (!capabilities.LlmOptions.Any(e => e.Provider == request.EvaluatorLlmProvider && e.Model == request.EvaluatorLlmModel)) throw new EntityValidationException("The selected evaluator LLM is unavailable.", nameof(request.EvaluatorLlmModel));
        if (!capabilities.EmbeddingOptions.Any(e => e.Provider == request.EvaluatorEmbeddingProvider && e.Model == request.EvaluatorEmbeddingModel)) throw new EntityValidationException("The selected evaluator embedding model is unavailable.", nameof(request.EvaluatorEmbeddingModel));
    }

    private static MetricComparisonDto Metric(string name, double? left, double? right, MetricCoverageDto leftCoverage, MetricCoverageDto rightCoverage) =>
        new() { Metric = name, LeftScore = left, RightScore = right, Delta = left.HasValue && right.HasValue ? right.Value - left.Value : null, LeftCoverage = leftCoverage, RightCoverage = rightCoverage };
}
