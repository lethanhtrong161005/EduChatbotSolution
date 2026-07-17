using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Utils;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Business.Services.AI.Indexing;

public sealed class SingleRunDocumentIndexingCoordinator(
    IDocumentParser parser,
    IDocumentChunkerSelector chunkerSelector,
    IEmbeddingService embedder,
    IDocumentFileService fileService,
    IAiConfigurationResolver aiConfigResolver,
    IUnitOfWork unitOfWork,
    IDocumentStatusRealtimeNotifier notifier,
    ILogger<SingleRunDocumentIndexingCoordinator> logger) : IDocumentIndexingCoordinator
{
    private const int BatchSize = 50;

    private readonly IDocumentParser _parser = parser;
    private readonly IDocumentChunkerSelector _chunkerSelector = chunkerSelector;
    private readonly IEmbeddingService _embedder = embedder;
    private readonly IDocumentFileService _fileService = fileService;
    private readonly IAiConfigurationResolver _aiConfigResolver = aiConfigResolver;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentStatusRealtimeNotifier _notifier = notifier;
    private readonly ILogger<SingleRunDocumentIndexingCoordinator> _logger = logger;

    public Task IndexAsync(Guid documentId, CancellationToken cxlTkn = default) => IndexCoreAsync(documentId, null, cxlTkn);
    public Task IndexAsync(Guid documentId, EffectiveAiConfiguration configuration, CancellationToken cxlTkn = default) => IndexCoreAsync(documentId, configuration, cxlTkn);

    private async Task IndexCoreAsync(Guid documentId, EffectiveAiConfiguration? config, CancellationToken cxlTkn)
    {
        var doc = await _unitOfWork.Documents.FindByIdAsync(documentId, cxlTkn)
            ?? throw new EntityNotFoundException("Could not find target document.");

        try
        {
            config ??= await _aiConfigResolver.GetAiConfigurationAsync(doc.SubjectId, cxlTkn);

            var sections = (await _unitOfWork.ParsedSections.GetAsync(
                filter: e => e.DocumentId == doc.Id,
                orderBy: q => q.OrderBy(e => e.SectionIndex),
                cancellationToken: cxlTkn))
                .ToList();

            var chunks = (await _unitOfWork.Chunks.GetAsync(
                filter: e => e.DocumentId == doc.Id,
                orderBy: q => q.OrderBy(e => e.ChunkIndex),
                cancellationToken: cxlTkn))
                .ToList();

            if (IsComplete(doc, sections, chunks, config))
            {
                if (doc.Status != DocumentStatus.Indexed)
                {
                    await MoveToDir(doc, FileDirectoryCategory.Indexed, cxlTkn);
                    await SaveAndPushUpdate(doc, DocumentStatus.Indexed, 100, chunkCount: chunks.Count, embeddingModel: doc.IndexedEmbeddingModel, cxlTkn: cxlTkn);
                }
                return;
            }

            doc.IndexingErrors = null;
            await MoveToDir(doc, FileDirectoryCategory.Processing, cxlTkn);

            if (!HasCompletedParsing(doc, sections, config)) sections = await ParseDocumentAsync(doc, sections, cxlTkn);
            if (!HasCompletedChunking(doc, chunks, config)) chunks = await ChunkDocumentAsync(doc, sections, chunks, config, cxlTkn);
            if (!HasCompletedEmbedding(doc, chunks, config))
            {
                SequentialIndexValidator.EnsureExact(chunks, e => e.ChunkIndex, "Stored document chunks");
                var chunksToEmbed = chunks.SkipWhile(e => e.Embedding != null && e.EmbeddingModel == config.EmbeddingModel).ToList();
                if (chunksToEmbed.Count > 0) await EmbedChunksAsync(doc, chunksToEmbed, config, cxlTkn);
            }

            await MoveToDir(doc, FileDirectoryCategory.Indexed, cxlTkn);

            doc.IndexedChunkingStrategy = config.ChunkingStrategy;
            doc.IndexedChunkSize = config.ChunkSize;
            doc.IndexedChunkOverlap = config.ChunkOverlap;
            doc.IndexedEmbeddingModel = config.EmbeddingModel;
            doc.IndexingErrors = null;

            await SaveAndPushUpdate(doc, DocumentStatus.Indexed, 100, chunkingStrategy: doc.IndexedChunkingStrategy, chunkCount: chunks.Count, embeddingModel: doc.IndexedEmbeddingModel, cxlTkn: cxlTkn);
        }
        catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await SaveFailure(doc, ex);
            throw;
        }
    }

    private async Task<List<ParsedSection>> ParseDocumentAsync(Document doc, List<ParsedSection> existingSections, CancellationToken cxlTkn)
    {
        doc.ParserUsed = _parser.ParserName;
        await SaveAndPushUpdate(doc, DocumentStatus.Parsing, parser: _parser.ParserName, cxlTkn: cxlTkn);

        var fileRead = await _fileService.OpenReadAsync(doc.Id, cxlTkn);
        if (!fileRead.Success) throw new FileNotFoundException(string.Join(Environment.NewLine, fileRead.Errors));

        await using var stream = fileRead.FileStream;
        var result = await _parser.ParseAsync(stream, doc.FileType, cxlTkn);
        var sections = result.Sections.OrderBy(e => e.SectionIndex).ToList();

        if (sections.Count == 0) throw new InvalidOperationException("The parser produced no document sections.");
        SequentialIndexValidator.EnsureExact(sections, e => e.SectionIndex, $"Sections produced by '{_parser.ParserName}'");

        foreach (var section in existingSections) _unitOfWork.ParsedSections.Delete(section);

        foreach (var section in sections)
        {
            section.DocumentId = doc.Id;
            _unitOfWork.ParsedSections.Insert(section);
        }

        await SaveAndPushUpdate(doc, DocumentStatus.Parsed, cxlTkn: cxlTkn);
        return sections;
    }

    private async Task<List<Chunk>> ChunkDocumentAsync(
        Document doc,
        List<ParsedSection> sections,
        List<Chunk> existingChunks,
        EffectiveAiConfiguration config,
        CancellationToken cxlTkn)
    {
        await SaveAndPushUpdate(doc, DocumentStatus.Chunking, chunkingStrategy: config.ChunkingStrategy, chunkCount: 0, cxlTkn: cxlTkn);

        SequentialIndexValidator.EnsureExact(sections, e => e.SectionIndex, "Stored document sections");
        var chunker = _chunkerSelector.Select(config.ChunkingStrategy);
        var results = chunker.Chunk(sections, new ChunkingOptions(config.ChunkSize, config.ChunkOverlap));
        if (results.Count == 0) throw new InvalidOperationException("The chunker produced no document chunks.");
        SequentialIndexValidator.EnsureExact(results, e => e.ChunkIndex, $"Chunks produced by '{chunker.StrategyName}'");

        cxlTkn.ThrowIfCancellationRequested();

        foreach (var chunk in existingChunks) _unitOfWork.Chunks.Delete(chunk);

        var chunks = results.Select(result => _unitOfWork.Chunks.Insert(new Chunk
        {
            DocumentId = doc.Id,
            ChunkIndex = result.ChunkIndex,
            ChunkText = result.ChunkText,
            StartPageNumber = result.StartPageNumber,
            EndPageNumber = result.EndPageNumber,
            StartSectionTitle = result.StartSectionTitle,
            EndSectionTitle = result.EndSectionTitle,
            ChunkingStrategy = chunker.StrategyName,
            ChunkSize = config.ChunkSize,
            ChunkOverlap = config.ChunkOverlap,
        })).ToList();

        await SaveAndPushUpdate(doc, DocumentStatus.Chunked, chunkingStrategy: config.ChunkingStrategy, chunkCount: chunks.Count, cxlTkn: cxlTkn);
        return chunks;
    }

    private async Task EmbedChunksAsync(Document doc, List<Chunk> chunks, EffectiveAiConfiguration config, CancellationToken cxlTkn)
    {
        await SaveAndPushUpdate(doc, DocumentStatus.Embedding, 0, embeddingModel: config.EmbeddingModel, cxlTkn: cxlTkn);

        var completed = 0;

        foreach (var batch in chunks.Chunk(BatchSize))
        {
            cxlTkn.ThrowIfCancellationRequested();

            var result = await _embedder.EmbedAsync(batch.Select(e => e.ChunkText), config.EmbeddingModel, cxlTkn);
            if (result.Vectors.Count != batch.Length) throw new InvalidOperationException($"Expected {batch.Length} embeddings, got {result.Vectors.Count}.");
            if (!string.Equals(result.Model, config.EmbeddingModel, StringComparison.Ordinal)) throw new InvalidOperationException($"Expected embedding model '{config.EmbeddingModel}', got '{result.Model}'.");

            for (var i = 0; i < batch.Length; i++)
            {
                batch[i].Embedding = new Vector(result.Vectors[i]);
                batch[i].EmbeddingModel = result.Model;
                batch[i].TokenCount = null; // TODO
                _unitOfWork.Chunks.Update(batch[i]);
            }

            completed += batch.Length;
            await SaveAndPushUpdate(doc, DocumentStatus.Embedding, 100d * completed / chunks.Count, embeddingModel: config.EmbeddingModel, cxlTkn: cxlTkn);
        }
    }

    private static bool IsComplete(Document doc, List<ParsedSection> sections, List<Chunk> chunks, EffectiveAiConfiguration config) =>
        HasCompletedParsing(doc, sections, config)
        && HasCompletedChunking(doc, chunks, config)
        && HasCompletedEmbedding(doc, chunks, config);

    private static bool HasCompletedParsing(Document doc, List<ParsedSection> sections, EffectiveAiConfiguration config) => sections.Count > 0 && SequentialIndexValidator.IsExact(sections, e => e.SectionIndex);

    private static bool HasCompletedChunking(Document doc, List<Chunk> chunks, EffectiveAiConfiguration config) =>
        chunks.Count > 0
        && SequentialIndexValidator.IsExact(chunks, e => e.ChunkIndex)
        && doc.IndexedChunkingStrategy == config.ChunkingStrategy
        && doc.IndexedChunkSize == config.ChunkSize
        && doc.IndexedChunkOverlap == config.ChunkOverlap
        && chunks.All(e => e.ChunkingStrategy == config.ChunkingStrategy
                           && e.ChunkSize == config.ChunkSize
                           && e.ChunkOverlap == config.ChunkOverlap);

    private static bool HasCompletedEmbedding(Document doc, List<Chunk> chunks, EffectiveAiConfiguration config) =>
        chunks.Count > 0
        && doc.IndexedEmbeddingModel == config.EmbeddingModel
        && chunks.All(e => e.Embedding != null
                           && e.EmbeddingModel == config.EmbeddingModel);

    private async Task MoveToDir(Document doc, FileDirectoryCategory directory, CancellationToken cxlTkn)
    {
        var result = await _fileService.MoveAsync(doc.Id, directory, cxlTkn);
        if (!result.Success) throw new IOException(string.Join(Environment.NewLine, result.Errors));
    }

    private async Task SaveAndPushUpdate(
        Document doc,
        DocumentStatus status,
        double? progress = null,
        string? parser = null,
        string? chunkingStrategy = null,
        int? chunkCount = null,
        string? embeddingModel = null,
        CancellationToken cxlTkn = default)
    {
        doc.Status = status;
        await _unitOfWork.SaveAsync(cxlTkn);

        try
        {
            await _notifier.PushUpdateAsync(new DocumentStatusUpdate
            {
                Id = doc.Id,
                Status = status,
                Progress = progress,
                ParserUsed = parser,
                ChunkingStrategy = chunkingStrategy,
                ChunkCount = chunkCount,
                EmbeddingModel = embeddingModel,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not publish indexing status for document {DocumentId}.", doc.Id);
        }
    }

    private async Task SaveFailure(Document doc, Exception exception)
    {
        doc.IndexingErrors = exception.ToString();

        try
        {
            await MoveToDir(doc, FileDirectoryCategory.Failed, CancellationToken.None);
        }
        catch (Exception moveException)
        {
            _logger.LogWarning(moveException, "Could not move failed document {DocumentId}.", doc.Id);
            doc.IndexingErrors += $"{Environment.NewLine}{Environment.NewLine}File move failure:{Environment.NewLine}{moveException}";
        }

        try
        {
            await SaveAndPushUpdate(doc, DocumentStatus.Failed, cxlTkn: CancellationToken.None);
        }
        catch (Exception saveException)
        {
            _logger.LogError(saveException, "Could not persist indexing failure for document {DocumentId}.", doc.Id);
        }
    }
}
