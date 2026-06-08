using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.DTOs;
using Domain.Entities;
using Domain.Exceptions;

namespace Business.Background;

public class DocumentIndexer(
    IDocumentParser parser,
    IDocumentChunker chunker,
    IEmbeddingService embedder,
    IUnitOfWork unitOfWork,
    IDocumentRealtimeNotifier documentRealtimeNotifier) : IDocumentIndexer
{
    private readonly IDocumentParser _parser = parser;
    private readonly IDocumentChunker _chunker = chunker;
    private readonly IEmbeddingService _embedder = embedder;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentRealtimeNotifier _docRtNotif = documentRealtimeNotifier;

    private readonly string _processingDir = Path.Combine(Path.GetTempPath(), AppConstants.AppDir, AppConstants.FileSubdirProcessing);
    private readonly string _indexedDir = Path.Combine(Path.GetTempPath(), AppConstants.AppDir, AppConstants.FileSubdirIndexed);
    private readonly string _failedDir = Path.Combine(Path.GetTempPath(), AppConstants.AppDir, AppConstants.FileSubdirFailed);

    private const int BatchSize = 50;

    public async Task ParseAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = await _unitOfWork.Documents.GetByIdAsync(documentId, cxlTkn)
                  ?? throw new EntityNotFoundException("Could not find the queued document.");

        if (doc.Status >= DocumentStatus.Parsed
            || await _unitOfWork.ParsedSections.ExistsAsync(e => e.DocumentId == doc.Id, cxlTkn))
            return;

        try
        {
            doc.IndexingErrors = null;
            MoveToDir(doc, _processingDir);
            doc.ParserUsed = _parser.ParserName;
            await SaveAndUpdate(doc, DocumentStatus.Parsing, parser: _parser.ParserName, cancellationToken: cxlTkn);

            var parsedDoc = await _parser.ParseAsync(doc.FilePath, doc.FileType, cxlTkn);

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
            throw;
        }
    }

    public async Task ChunkAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(filter: e => e.Id == documentId,
                                                        includeProperties: [nameof(Document.ParsedSections)],
                                                        cancellationToken: cxlTkn))
                                              .FirstOrDefault()
                  ?? throw new EntityNotFoundException("Could not find the queued document.");

        if (doc.Status >= DocumentStatus.Chunked
            || await _unitOfWork.Chunks.ExistsAsync(e => e.DocumentId == doc.Id, cxlTkn))
            return;

        try
        {
            doc.IndexingErrors = null;
            MoveToDir(doc, _processingDir);
            await SaveAndUpdate(doc, DocumentStatus.Chunking, chunkCount: 0, cancellationToken: cxlTkn);

            var sections = doc.ParsedSections.OrderBy(e => e.SectionIndex);
            var totalSectionCount = sections.Count();
            var sectionCount = 0;
            var chunkCount = 0;

            foreach (var section in sections)
            {
                cxlTkn.ThrowIfCancellationRequested();

                sectionCount++;

                foreach (var chunkDto in _chunker.Chunk(section))
                {
                    chunkCount++;

                    _unitOfWork.Chunks.Insert(new Chunk
                    {
                        DocumentId = doc.Id,
                        ChunkIndex = chunkDto.ChunkIndex,
                        ChunkText = chunkDto.ChunkText,
                        PageNumber = chunkDto.PageNumber,
                        SectionTitle = chunkDto.SectionTitle,
                        ChunkStrategy = _chunker.ChunkStrategy,
                    });
                }

                _ = _docRtNotif.UpdateStatus(new DocumentStatusUpdate
                {
                    Id = doc.Id,
                    Status = DocumentStatus.Chunking,
                    Progress = 100d * sectionCount / totalSectionCount,
                    ChunkCount = chunkCount,
                });
            }

            await SaveAndUpdate(doc, DocumentStatus.Chunked, chunkCount: chunkCount, cancellationToken: cxlTkn);
        }
        catch (Exception ex)
        {
            await SaveFailure(doc, ex, cxlTkn);
            throw;
        }
    }

    public async Task EmbedAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(filter: e => e.Id == documentId,
                                                        includeProperties: [nameof(Document.Chunks)],
                                                        cancellationToken: cxlTkn))
                                              .FirstOrDefault()
                  ?? throw new EntityNotFoundException("Could not find the queued document.");

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
            MoveToDir(doc, _indexedDir);
            await SaveAndUpdate(doc, DocumentStatus.Indexed, cancellationToken: cxlTkn);
            return;
        }

        try
        {
            var total = doc.Chunks.Count;
            var cur = total - pendingChunks.Count;
            var progress = 100d * cur / total;

            doc.IndexingErrors = null;
            MoveToDir(doc, _processingDir);
            await SaveAndUpdate(doc, DocumentStatus.Embedding, progress, embeddingModel: _embedder.ModelName, cancellationToken: cxlTkn);

            foreach (var batch in pendingChunks.Chunk(BatchSize))
            {
                var result = await _embedder.EmbedAsync(batch.Select(e => e.ChunkText), cxlTkn);

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

            MoveToDir(doc, _indexedDir);
            await SaveAndUpdate(doc, DocumentStatus.Indexed, cancellationToken: cxlTkn);
        }
        catch (Exception ex)
        {
            await SaveFailure(doc, ex, cxlTkn);
            throw;
        }
    }

    private static void MoveToDir(Document doc, string dir)
    {
        if (!File.Exists(doc.FilePath))
        {
            throw new FileNotFoundException($"Could not locate document file at '{doc.FilePath}'");
        }

        if (doc.FilePath != dir)
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, doc.FileName);
            File.Move(doc.FilePath, path);
            doc.FilePath = path;
        }
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

        _ = _docRtNotif.UpdateStatus(docStatusUpd);
    }

    private async Task SaveFailure(
        Document doc,
        Exception ex,
        CancellationToken cxlTkn)
    {
        doc.IndexingErrors = ex.ToString();
        MoveToDir(doc, _failedDir);
        await SaveAndUpdate(doc, DocumentStatus.Failed, cancellationToken: cxlTkn);
    }
}
