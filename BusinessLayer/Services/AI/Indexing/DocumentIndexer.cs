using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Business.Services.AI.Indexing;

public sealed class DocumentIndexer(
    IDocumentParser parser,
    IDocumentChunkerSelector chunkerSelector,
    IEmbeddingService embedder,
    IDocumentFileService fileService,
    IAiConfigurationResolver aiConfigResolver,
    IUnitOfWork unitOfWork,
    IDocumentStatusRealtimeNotifier notifier,
    ILogger<DocumentIndexer> logger) : IDocumentIndexer
{
    private const int BatchSize = 50;

    private readonly IDocumentParser _parser = parser;
    private readonly IDocumentChunkerSelector _chunkerSelector = chunkerSelector;
    private readonly IEmbeddingService _embedder = embedder;
    private readonly IDocumentFileService _fileService = fileService;
    private readonly IAiConfigurationResolver _aiConfigResolver = aiConfigResolver;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentStatusRealtimeNotifier _notifier = notifier;
    private readonly ILogger<DocumentIndexer> _logger = logger;

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

            if (IsComplete(doc, chunks, config))
            {
                if (doc.Status != DocumentStatus.Indexed)
                {
                    await MoveToDir(doc, DocumentFileDirectory.Indexed, cxlTkn);
                    await SaveAndUpdate(doc, DocumentStatus.Indexed, 100, chunkCount: chunks.Count, embeddingModel: config.EmbeddingModel, cancellationToken: cxlTkn);
                }
                return;
            }

            doc.IndexingErrors = null;
            await MoveToDir(doc, DocumentFileDirectory.Processing, cxlTkn);

            if (sections.Count == 0) sections = await ParseDocumentAsync(doc, cxlTkn);
            chunks = await ChunkDocumentAsync(doc, sections, chunks, config, cxlTkn);
            await EmbedChunksAsync(doc, chunks, config, cxlTkn);

            await MoveToDir(doc, DocumentFileDirectory.Indexed, cxlTkn);

            doc.IndexedChunkingStrategy = config.ChunkingStrategy;
            doc.IndexedChunkSize = config.ChunkSize;
            doc.IndexedChunkOverlap = config.ChunkOverlap;
            doc.IndexedEmbeddingModel = config.EmbeddingModel;
            doc.IndexingErrors = null;

            await SaveAndUpdate(doc, DocumentStatus.Indexed, 100, chunkingStrategy: config.ChunkingStrategy, chunkCount: chunks.Count, embeddingModel: config.EmbeddingModel, cancellationToken: cxlTkn);
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

    private async Task<List<ParsedSection>> ParseDocumentAsync(Document doc, CancellationToken cxlTkn)
    {
        doc.ParserUsed = _parser.ParserName;
        await SaveAndUpdate(doc, DocumentStatus.Parsing, parser: _parser.ParserName, cancellationToken: cxlTkn);

        var fileRead = await _fileService.OpenReadAsync(doc.Id, cxlTkn);
        if (!fileRead.Success) throw new FileNotFoundException(string.Join(Environment.NewLine, fileRead.Errors));

        await using var stream = fileRead.FileStream;
        var result = await _parser.ParseAsync(stream, doc.FileType, cxlTkn);
        var sections = result.Sections.OrderBy(e => e.SectionIndex).ToList();

        if (sections.Count == 0) throw new InvalidOperationException("The parser produced no document sections.");

        foreach (var section in sections)
        {
            section.DocumentId = doc.Id;
            _unitOfWork.ParsedSections.Insert(section);
        }

        await SaveAndUpdate(doc, DocumentStatus.Parsed, cancellationToken: cxlTkn);
        return sections;
    }

    private async Task<List<Chunk>> ChunkDocumentAsync(
        Document doc,
        List<ParsedSection> sections,
        List<Chunk> existingChunks,
        EffectiveAiConfiguration config,
        CancellationToken cxlTkn)
    {
        await SaveAndUpdate(doc, DocumentStatus.Chunking, chunkingStrategy: config.ChunkingStrategy, chunkCount: 0, cancellationToken: cxlTkn);

        var chunker = _chunkerSelector.Select(config.ChunkingStrategy);
        var results = chunker.Chunk(sections, new ChunkingOptions(config.ChunkSize, config.ChunkOverlap));
        if (results.Count == 0) throw new InvalidOperationException("The chunker produced no document chunks.");

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
            ChunkStrategy = chunker.StrategyName,
        })).ToList();

        await SaveAndUpdate(doc, DocumentStatus.Chunked, chunkingStrategy: config.ChunkingStrategy, chunkCount: chunks.Count, cancellationToken: cxlTkn);
        return chunks;
    }

    private async Task EmbedChunksAsync(Document doc, List<Chunk> chunks, EffectiveAiConfiguration config, CancellationToken cxlTkn)
    {
        await SaveAndUpdate(doc, DocumentStatus.Embedding, 0, chunkCount: chunks.Count, embeddingModel: config.EmbeddingModel, cancellationToken: cxlTkn);

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
            await SaveAndUpdate(doc, DocumentStatus.Embedding, 100d * completed / chunks.Count, chunkCount: chunks.Count, embeddingModel: config.EmbeddingModel, cancellationToken: cxlTkn);
        }
    }

    private static bool IsComplete(Document doc, List<Chunk> chunks, EffectiveAiConfiguration config) =>
        chunks.Count > 0
        && doc.IndexedChunkingStrategy == config.ChunkingStrategy
        && doc.IndexedChunkSize == config.ChunkSize
        && doc.IndexedChunkOverlap == config.ChunkOverlap
        && doc.IndexedEmbeddingModel == config.EmbeddingModel
        && chunks.All(e => e.Embedding != null
                           && e.ChunkStrategy == config.ChunkingStrategy
                           && e.EmbeddingModel == config.EmbeddingModel);

    private async Task MoveToDir(Document doc, DocumentFileDirectory directory, CancellationToken cxlTkn)
    {
        var result = await _fileService.MoveAsync(doc.Id, directory, cxlTkn);
        if (!result.Success) throw new IOException(string.Join(Environment.NewLine, result.Errors));
    }

    private async Task SaveAndUpdate(
        Document doc,
        DocumentStatus status,
        double? progress = null,
        string? parser = null,
        string? chunkingStrategy = null,
        int? chunkCount = null,
        string? embeddingModel = null,
        CancellationToken cancellationToken = default)
    {
        doc.Status = status;
        await _unitOfWork.SaveAsync(cancellationToken);

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
            await MoveToDir(doc, DocumentFileDirectory.Failed, CancellationToken.None);
        }
        catch (Exception moveException)
        {
            _logger.LogWarning(moveException, "Could not move failed document {DocumentId}.", doc.Id);
            doc.IndexingErrors += $"{Environment.NewLine}{Environment.NewLine}File move failure:{Environment.NewLine}{moveException}";
        }

        try
        {
            await SaveAndUpdate(doc, DocumentStatus.Failed, cancellationToken: CancellationToken.None);
        }
        catch (Exception saveException)
        {
            _logger.LogError(saveException, "Could not persist indexing failure for document {DocumentId}.", doc.Id);
        }
    }
}
