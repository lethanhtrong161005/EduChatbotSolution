using Domain.Contracts;
using Domain.DTOs;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace Business.Embedding;

public class OllamaEmbeddingService(
    IOllamaApiClient client,
    IOptions<OllamaOptions> options)
    : IEmbeddingService
{
    private readonly IOllamaApiClient _client = client;
    private readonly OllamaOptions _options = options.Value;

    public string ModelName => _options.EmbeddingModel;

    public async Task<EmbeddingResult> EmbedAsync(IEnumerable<string> texts, CancellationToken cxlTkn = default)
    {
        var response = await _client.EmbedAsync(
            new EmbedRequest
            {
                Model = _options.EmbeddingModel,
                Input = [.. texts],
            }, cxlTkn);

        return new EmbeddingResult
        {
            Model = ModelName,
            Vectors = response.Embeddings,
            TokenCount = response.PromptEvalCount,
        };
    }
}
