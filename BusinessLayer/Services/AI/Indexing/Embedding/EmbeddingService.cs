using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.Extensions.AI;

namespace Business.Services.AI.Indexing.Embedding;

public class EmbeddingService(
    IEmbeddingGeneratorFactory factory)
    : IEmbeddingService
{
    private readonly IEmbeddingGeneratorFactory _factory = factory;

    public async Task<EmbedResult> EmbedAsync(IEnumerable<string> texts, string modelName, CancellationToken cxlTkn = default)
    {
        var generator = _factory.GetEmbeddingGenerator(modelName);

        var result = await generator.GenerateAsync(
            texts,
            new EmbeddingGenerationOptions
            {
                ModelId = modelName,
            },
            cxlTkn);

        return new EmbedResult
        {
            Model = modelName,
            Vectors = [.. result.Select(x => x.Vector)],
            InputTokenCount = result.Usage?.InputTokenCount,
            OutputTokenCount = result.Usage?.OutputTokenCount,
        };
    }
}

