using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;

namespace Business.Services.AI.Indexing;

public class DocumentIndexer(
    IDocumentParser parser,
    IDocumentChunker chunker,
    IEmbeddingService embedder,
    IDocumentFileService fileService,
    IAiConfigurationResolver aiConfigResolver,
    IUnitOfWork unitOfWork,
    IDocumentStatusRealtimeNotifier notifier)
    : IDocumentIndexer
{
    private readonly IDocumentParser _parser = parser;
    private readonly IDocumentChunker _chunker = chunker;
    private readonly IEmbeddingService _embedder = embedder;
    private readonly IDocumentFileService _fileService = fileService;
    private readonly IAiConfigurationResolver _aiConfigResolver = aiConfigResolver;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentStatusRealtimeNotifier _notifier = notifier;

    private const int BatchSize = 50;

    public async Task ParseAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = await _unitOfWork.Documents.FindByIdAsync(documentId, cxlTkn)
                  ?? throw new EntityNotFoundException("Could not find target document.");

        if (doc.Status >= DocumentStatus.Parsed
            || await _unitOfWork.ParsedSections.ExistsAsync(e => e.DocumentId == doc.Id, cxlTkn))
            return;

        try
        {
            doc.IndexingErrors = null;

            await MoveToDir(doc, DocumentFileDirectory.Processing);

            var result = await _fileService.Download(doc.Id, cxlTkn);
            if (!result.Success)
                throw new FileNotFoundException("Failed to download document file.");

            doc.ParserUsed = _parser.ParserName;
            await SaveAndUpdate(doc, DocumentStatus.Parsing, parser: _parser.ParserName, cancellationToken: cxlTkn);

            var parsedDoc = await _parser.ParseAsync(result.FilePath, doc.FileType, cxlTkn);

            foreach (var section in parsedDoc.Sections)
            {
                section.DocumentId = doc.Id;
                _unitOfWork.ParsedSections.Insert(section);
            }

            await SaveAndUpdate(doc, DocumentStatus.Parsed, cancellationToken: cxlTkn);
        }
        catch (Exception ex)
        {
            await SaveFailure(doc, ex, cxlTkn);
        }
    }

    public async Task ChunkAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(filter: e => e.Id == documentId,
                                                        includeProperties: [nameof(Document.ParsedSections), nameof(Document.Chapter)],
                                                        cancellationToken: cxlTkn))
                                              .FirstOrDefault()
                  ?? throw new EntityNotFoundException("Could not find target document.");

        if (doc.Status >= DocumentStatus.Chunked
            || await _unitOfWork.Chunks.ExistsAsync(e => e.DocumentId == doc.Id, cxlTkn))
            return;

        try
        {
            doc.IndexingErrors = null;
            await MoveToDir(doc, DocumentFileDirectory.Processing);
            await SaveAndUpdate(doc, DocumentStatus.Chunking, chunkCount: 0, cancellationToken: cxlTkn);

            var sections = doc.ParsedSections.OrderBy(e => e.SectionIndex);
            var totalSectionCount = sections.Count();
            var sectionCount = 0;
            var chunkCount = 0;

            var aiConfig = await _aiConfigResolver.GetAiConfigurationAsync(doc.Chapter.SubjectId, cxlTkn);

            // TODO: Swap _chunker for ChunkingService -> Use strategy pattern

            foreach (var section in sections)
            {
                cxlTkn.ThrowIfCancellationRequested();

                sectionCount++;

                foreach (var chunkRes in _chunker.Chunk(section, startIndex: chunkCount))
                {
                    chunkCount++;

                    _unitOfWork.Chunks.Insert(new Chunk
                    {
                        DocumentId = doc.Id,
                        ChunkIndex = chunkRes.ChunkIndex,
                        ChunkText = chunkRes.ChunkText,
                        PageNumber = chunkRes.PageNumber,
                        SectionTitle = chunkRes.SectionTitle,
                        ChunkStrategy = aiConfig.ChunkingStrategy,
                    });
                }

                _ = _notifier.PushUpdateAsync(new DocumentStatusUpdate
                {
                    Id = doc.Id,
                    Status = DocumentStatus.Chunking,
                    Progress = 100d * sectionCount / totalSectionCount,
                    ChunkCount = chunkCount,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            await SaveAndUpdate(doc, DocumentStatus.Chunked, chunkCount: chunkCount, cancellationToken: cxlTkn);
        }
        catch (Exception ex)
        {
            await SaveFailure(doc, ex, cxlTkn);
        }
    }

    public async Task EmbedAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(filter: e => e.Id == documentId,
                                                        includeProperties: [nameof(Document.Chunks), nameof(Document.Chapter)],
                                                        cancellationToken: cxlTkn))
                                              .FirstOrDefault()
                  ?? throw new EntityNotFoundException("Could not find target document.");

        if (doc.Status >= DocumentStatus.Indexed
            && doc.Chunks.All(c => c.Embedding != null))
            return;

        var pendingChunks = doc.Chunks
            .Where(e => e.Embedding == null)
            .OrderBy(e => e.ChunkIndex)
            .ToList();

        if (pendingChunks.Count == 0)
        {
            // TODO: Log WARN
            doc.IndexingErrors = null;
            await MoveToDir(doc, DocumentFileDirectory.Indexed);
            await SaveAndUpdate(doc, DocumentStatus.Indexed, cancellationToken: cxlTkn);
            return;
        }

        var aiConfig = await _aiConfigResolver.GetAiConfigurationAsync(doc.Chapter.SubjectId, cxlTkn);

        try
        {
            var total = doc.Chunks.Count;
            var cur = total - pendingChunks.Count;
            var progress = 100d * cur / total;

            doc.IndexingErrors = null;
            await MoveToDir(doc, DocumentFileDirectory.Processing);
            await SaveAndUpdate(doc, DocumentStatus.Embedding, progress, embeddingModel: aiConfig.EmbeddingModel, cancellationToken: cxlTkn);

            foreach (var batch in pendingChunks.Chunk(BatchSize))
            {
                var result = await _embedder.EmbedAsync(batch.Select(e => e.ChunkText), aiConfig.EmbeddingModel, cxlTkn);

                if (result.Vectors.Count != batch.Length)
                {
                    throw new InvalidOperationException(
                        $"Expected {batch.Length} embeddings, got {result.Vectors.Count}.");
                }

                for (int i = 0; i < batch.Length; i++)
                {
                    var chunk = batch[i];

                    chunk.Embedding = new Pgvector.Vector(result.Vectors[i]);
                    chunk.EmbeddingModel = result.Model;
                    chunk.TokenCount = null; // TODO: Tokenizer service

                    _unitOfWork.Chunks.Update(chunk);
                }

                cur += batch.Length;
                progress = 100d * cur / total;
                await SaveAndUpdate(doc, DocumentStatus.Embedding, progress, cancellationToken: cxlTkn);
            }

            await MoveToDir(doc, DocumentFileDirectory.Indexed);
            await SaveAndUpdate(doc, DocumentStatus.Indexed, cancellationToken: cxlTkn);
        }
        catch (Exception ex)
        {
            await SaveFailure(doc, ex, cxlTkn);
        }
    }

    private async Task MoveToDir(Document doc, DocumentFileDirectory dir)
    {
        if (!await _fileService.Exists(doc.Id))
            throw new FileNotFoundException($"Could not locate document file at '{doc.FilePath}'");
        await _fileService.Move(doc.Id, dir);
    }

    private async Task SaveAndUpdate(
        Document doc,
        DocumentStatus docStatus,
        double? progress = null,
        string? parser = null,
        int? chunkCount = null,
        string? embeddingModel = null,
        CancellationToken cancellationToken = default)
    {
        doc.Status = docStatus;
        await _unitOfWork.SaveAsync(cancellationToken);

        var docStatusUpd = new DocumentStatusUpdate
        {
            Id = doc.Id,
            Status = docStatus,
            Progress = progress,
            ParserUsed = parser,
            ChunkCount = chunkCount,
            EmbeddingModel = embeddingModel,
            UpdatedAt = DateTime.UtcNow,
        };

        await _notifier.PushUpdateAsync(docStatusUpd);
    }

    private async Task SaveFailure(
        Document doc,
        Exception ex,
        CancellationToken cxlTkn)
    {
        doc.IndexingErrors = ex.ToString();
        await MoveToDir(doc, DocumentFileDirectory.Failed);
        await SaveAndUpdate(doc, DocumentStatus.Failed, cancellationToken: cxlTkn);
    }
}
