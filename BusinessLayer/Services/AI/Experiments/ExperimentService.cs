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
    IMapper mapper)
    : IExperimentService
{
    private const string DatasetKey = "db201-vi-50-v1";
    private const string EvaluatorPromptVersion = "ragas-style-v1";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiConfigurationResolver _configurationResolver = configurationResolver;
    private readonly IAiConfigurationAdminService _configurationAdminService = configurationAdminService;
    private readonly IExperimentDatasetProvider _datasetProvider = datasetProvider;
    private readonly IExperimentDispatcher _dispatcher = dispatcher;
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
        if (subject.Code != "DB201") throw new EntityValidationException("Sprint experiments are restricted to the DB201 subject.", nameof(request.SubjectId));
        if (subject.IndexAvailability == SubjectIndexAvailability.Reindexing) throw new EntityConflictException("The subject is currently being reindexed.", nameof(Subject.IndexAvailability));

        await _datasetProvider.ImportAsync(cxlTkn);

        var requestedIds = request.TestQuestionIds.Distinct().Order().ToArray();
        var questions = (await _unitOfWork.TestQuestions.GetAsync(
            filter: e => e.SubjectId == request.SubjectId && requestedIds.Contains(e.Id),
            orderBy: q => q.OrderBy(e => e.ExternalId),
            asNoTracking: true,
            cancellationToken: cxlTkn)).ToList();

        if (questions.Count != requestedIds.Length) throw new EntityValidationException("One or more selected questions do not belong to the requested subject.", nameof(request.TestQuestionIds));

        var current = await _configurationResolver.GetAiConfigurationAsync(request.SubjectId, cxlTkn);
        var affected = await _unitOfWork.ExperimentRuns.CountAffectedDocumentsAsync(request.SubjectId, request.ChunkingStrategy, request.ChunkSize, request.ChunkOverlap, request.EmbeddingModel, cxlTkn);
        var now = DateTime.UtcNow;
        var experiment = new Experiment
        {
            ExperimentName = request.ExperimentName.Trim(),
            SubjectId = request.SubjectId,
            Status = ExperimentStatus.Queued,
            QuestionSetKey = CreateQuestionSetKey(questions),
            AffectedDocumentCount = affected,
            IndexedDocumentCount = 0,
            CompletedQuestionCount = 0,
            TotalQuestionCount = questions.Count,
            Notes = request.Notes?.Trim(),
            ConfigurationSnapshot = new ExperimentConfigurationSnapshot
            {
                SubjectId = subject.Id,
                SubjectCode = subject.Code,
                SubjectName = subject.Name,
                ChunkingStrategy = request.ChunkingStrategy,
                ChunkSize = request.ChunkSize,
                ChunkOverlap = request.ChunkOverlap,
                EmbeddingModel = request.EmbeddingModel,
                TopK = request.TopK,
                SimilarityThreshold = request.SimilarityThreshold,
                MaxContextChunks = request.MaxContextChunks,
                LlmModel = request.LlmModel,
                ChatTemperature = request.ChatTemperature,
                MaxHistoryMessages = current.MaxHistoryMessages,
                ChatPrompt = current.ChatPrompt,
                ContextPrompt = current.ContextPrompt,
                NoContextRetrievedPrompt = current.NoContextRetrievedPrompt,
                JudgeModel = request.JudgeModel,
                EvaluatorPromptVersion = EvaluatorPromptVersion,
            },
        };

        foreach (var question in questions) experiment.TestResponses.Add(new TestResponse
        {
            TestQuestionId = question.Id,
            Status = ExperimentQuestionStatus.Pending
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
            includeProperties: [nameof(Experiment.Subject), nameof(Experiment.ConfigurationSnapshot)],
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
            Questions = [.. experiment.TestResponses.OrderBy(e => e.TestQuestion.ExternalId).Select(_mapper.Map<ExperimentQuestionResultDto>)],
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

        var rightQuestions = right.Questions.ToDictionary(e => e.TestQuestionId);
        return new ExperimentComparisonDto
        {
            Left = left.Summary,
            Right = right.Summary,
            Metrics =
            [
                Metric("Faithfulness", left.Summary.AggregateScores.Faithfulness, right.Summary.AggregateScores.Faithfulness),
                Metric("Answer relevancy", left.Summary.AggregateScores.AnswerRelevancy, right.Summary.AggregateScores.AnswerRelevancy),
                Metric("Context precision", left.Summary.AggregateScores.ContextPrecision, right.Summary.AggregateScores.ContextPrecision),
                Metric("Context recall", left.Summary.AggregateScores.ContextRecall, right.Summary.AggregateScores.ContextRecall),
            ],
            Questions =
            [
                .. left.Questions.OrderBy(e => e.ExternalId).Select(question =>
                {
                    var other = rightQuestions[question.TestQuestionId];
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
        if (request.JudgeModel != ChatModelName.Gemini35Flash) throw new EntityValidationException($"Judge model must be '{ChatModelName.Gemini35Flash}'.", nameof(request.JudgeModel));
    }

    private static void ValidateIndexing(int subjectId, string strategy, int size, int overlap, string embeddingModel)
    {
        if (subjectId <= 0) throw new EntityValidationException("Subject ID must be positive.", nameof(subjectId));
        if (strategy is not (ChunkingStrategy.FixedLength or ChunkingStrategy.RecursiveSeparator or ChunkingStrategy.SentenceParagraph)) throw new EntityValidationException($"Unknown chunking strategy '{strategy}'.", nameof(strategy));
        if (size is < 100 or > 8000) throw new EntityValidationException("Chunk size must be between 100 and 8000.", nameof(size));
        if (overlap < 0 || overlap >= size) throw new EntityValidationException("Chunk overlap must be non-negative and smaller than chunk size.", nameof(overlap));
        if (embeddingModel is not (EmbeddingModelName.BgeM3 or EmbeddingModelName.NemotronEmbedVLFree or EmbeddingModelName.GeminiEmbedding2)) throw new EntityValidationException($"Unknown embedding model '{embeddingModel}'.", nameof(embeddingModel));
    }

    private static string CreateQuestionSetKey(IEnumerable<TestQuestion> questions)
    {
        var value = $"{DatasetKey}:{string.Join(',', questions.Select(e => e.ExternalId).Order(StringComparer.Ordinal))}";
        return $"{DatasetKey}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16]}";
    }

    //// FIXME: Use AutoMapper mappings.

    //private static ExperimentSummaryDto MapSummary(Experiment experiment) => new()
    //{
    //    ExperimentId = experiment.Id,
    //    ExperimentName = experiment.ExperimentName,
    //    SubjectId = experiment.SubjectId,
    //    SubjectCode = experiment.Subject.Code,
    //    Status = experiment.Status,
    //    ChunkingStrategy = experiment.ConfigurationSnapshot.ChunkingStrategy,
    //    ChunkSize = experiment.ConfigurationSnapshot.ChunkSize,
    //    ChunkOverlap = experiment.ConfigurationSnapshot.ChunkOverlap,
    //    EmbeddingModel = experiment.ConfigurationSnapshot.EmbeddingModel,
    //    LlmModel = experiment.ConfigurationSnapshot.LlmModel,
    //    QuestionSetKey = experiment.QuestionSetKey,
    //    IndexedDocumentCount = experiment.IndexedDocumentCount,
    //    AffectedDocumentCount = experiment.AffectedDocumentCount,
    //    CompletedQuestionCount = experiment.CompletedQuestionCount,
    //    TotalQuestionCount = experiment.TotalQuestionCount,
    //    AggregateScores = Scores(experiment.Faithfulness, experiment.AnswerRelevancy, experiment.ContextPrecision, experiment.ContextRecall),
    //    CreatedAt = experiment.CreatedAt,
    //    CompletedAt = experiment.CompletedAt,
    //    FailureReason = experiment.FailureReason,
    //};

    //private static ExperimentConfigurationSnapshotDto MapConfiguration(ExperimentConfigurationSnapshot snapshot) => new()
    //{
    //    SubjectId = snapshot.SubjectId,
    //    SubjectCode = snapshot.SubjectCode,
    //    SubjectName = snapshot.SubjectName,
    //    ChunkingStrategy = snapshot.ChunkingStrategy,
    //    ChunkSize = snapshot.ChunkSize,
    //    ChunkOverlap = snapshot.ChunkOverlap,
    //    EmbeddingModel = snapshot.EmbeddingModel,
    //    TopK = snapshot.TopK,
    //    SimilarityThreshold = snapshot.SimilarityThreshold,
    //    MaxContextChunks = snapshot.MaxContextChunks,
    //    LlmModel = snapshot.LlmModel,
    //    ChatTemperature = snapshot.ChatTemperature,
    //    MaxHistoryMessages = snapshot.MaxHistoryMessages,
    //    ChatPrompt = snapshot.ChatPrompt,
    //    ContextPrompt = snapshot.ContextPrompt,
    //    NoContextRetrievedPrompt = snapshot.NoContextRetrievedPrompt,
    //    JudgeModel = snapshot.JudgeModel,
    //    EvaluatorPromptVersion = snapshot.EvaluatorPromptVersion,
    //};

    //private static ExperimentQuestionResultDto MapQuestion(TestResponse response) => new()
    //{
    //    TestResponseId = response.Id,
    //    TestQuestionId = response.TestQuestionId,
    //    ExternalId = response.TestQuestion.ExternalId,
    //    Question = response.TestQuestion.Question,
    //    GroundTruth = response.TestQuestion.GroundTruth,
    //    Status = response.Status,
    //    GeneratedAnswer = response.GeneratedAnswer,
    //    RetrievedContexts = [.. response.RetrievedContexts.OrderBy(e => e.ContextIndex).Select(e => e.ContextText)],
    //    Scores = Scores(response.Faithfulness, response.AnswerRelevancy, response.ContextPrecision, response.ContextRecall),
    //    Explanation = response.Explanation,
    //    FailureReason = response.FailureReason,
    //    PromptTokens = response.PromptTokens,
    //    CompletionTokens = response.CompletionTokens,
    //    RetrievalTimeMs = response.RetrievalTimeMs,
    //    TimeToFirstTokenMs = response.TimeToFirstTokenMs,
    //    TotalResponseTimeMs = response.TotalResponseTimeMs,
    //};

    private static RagasStyleScoresDto Scores(double? faithfulness, double? answerRelevancy, double? contextPrecision, double? contextRecall) =>
        new() { Faithfulness = faithfulness, AnswerRelevancy = answerRelevancy, ContextPrecision = contextPrecision, ContextRecall = contextRecall };

    private static MetricComparisonDto Metric(string name, double? left, double? right) =>
        new() { Metric = name, LeftScore = left, RightScore = right, Delta = left.HasValue && right.HasValue ? right.Value - left.Value : null };
}
