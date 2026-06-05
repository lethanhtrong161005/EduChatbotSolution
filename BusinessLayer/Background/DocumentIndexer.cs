using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Background;

public class DocumentIndexer(
    IDocumentParser parser,
    IDocumentChunker chunker,
    IEmbeddingService embedder,
    IUnitOfWork unitOfWork) : IDocumentIndexer
{
    private readonly IDocumentParser _parser = parser;
    private readonly IDocumentChunker _chunker = chunker;
    private readonly IEmbeddingService _embedder = embedder;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private const int BatchSize = 32;

    public async Task ParseAsync(Guid documentId)
    {
        var doc = await _unitOfWork.Documents.GetByIdAsync(documentId)
                  ?? throw new Exception("Could not find the queued document.");

        if (doc.Status >= DocumentStatus.Parsed
            || await _unitOfWork.ParsedSections.ExistsAsync(e => e.DocumentId == doc.Id))
            return;

        try
        {
            doc.IndexingErrors = null;
            doc.Status = DocumentStatus.Parsing;
            await _unitOfWork.SaveAsync();

            var parsedDoc = await _parser.ParseAsync(doc.FilePath, doc.FileType);

            foreach (var section in parsedDoc.Sections)
            {
                section.DocumentId = doc.Id;
                _unitOfWork.ParsedSections.Insert(section);
            }

            doc.ParserUsed = _parser.ParserName;
            doc.Status = DocumentStatus.Parsed;
            await _unitOfWork.SaveAsync();
        }
        catch (Exception ex)
        {
            doc.Status = DocumentStatus.Failed;
            doc.IndexingErrors = ex.ToString();
            await _unitOfWork.SaveAsync();
            throw;
        }
    }

    public async Task ChunkAsync(Guid documentId)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(filter: e => e.Id == documentId,
                                                        includeProperties: [nameof(Document.ParsedSections)]))
                                              .FirstOrDefault()
                  ?? throw new Exception("Could not find the queued document.");

        if (doc.Status >= DocumentStatus.Chunked
            || await _unitOfWork.Chunks.ExistsAsync(e => e.DocumentId == doc.Id))
            return;

        try
        {
            doc.IndexingErrors = null;
            doc.Status = DocumentStatus.Chunking;
            await _unitOfWork.SaveAsync();

            foreach (var chunkDto in _chunker.Chunk(doc.ParsedSections))
            {
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

            doc.Status = DocumentStatus.Chunked;
            await _unitOfWork.SaveAsync();
        }
        catch (Exception ex)
        {
            doc.Status = DocumentStatus.Failed;
            doc.IndexingErrors = ex.ToString();
            await _unitOfWork.SaveAsync();
            throw;
        }
    }

    public async Task EmbedAsync(Guid documentId)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(filter: e => e.Id == documentId,
                                                        includeProperties: [nameof(Document.Chunks)]))
                                              .FirstOrDefault()
                  ?? throw new Exception("Could not find the queued document.");

        if (doc.Status >= DocumentStatus.Indexed)
            return;

        try
        {
            doc.IndexingErrors = null;
            doc.Status = DocumentStatus.Embedding;
            await _unitOfWork.SaveAsync();

            var pendingChunks = doc.Chunks
                .Where(e => e.Embedding == null)
                .OrderBy(e => e.ChunkIndex)
                .ToList();

            foreach (var batch in pendingChunks.Chunk(BatchSize))
            {
                var result = await _embedder.EmbedAsync(batch.Select(e => e.ChunkText));

                for (int i = 0; i < batch.Length; i++)
                {
                    var chunk = batch[i];

                    chunk.Embedding = new Pgvector.Vector(result.Vectors[i]);
                    chunk.EmbeddingModel = result.Model;
                    chunk.TokenCount = null; // TODO: Tokenizer service

                    _unitOfWork.Chunks.Update(chunk);
                }

                await _unitOfWork.SaveAsync();
            }

            doc.Status = DocumentStatus.Indexed;
            await _unitOfWork.SaveAsync();
        }
        catch (Exception ex)
        {
            doc.Status = DocumentStatus.Failed;
            doc.IndexingErrors = ex.ToString();
            await _unitOfWork.SaveAsync();
            throw;
        }
    }
}
