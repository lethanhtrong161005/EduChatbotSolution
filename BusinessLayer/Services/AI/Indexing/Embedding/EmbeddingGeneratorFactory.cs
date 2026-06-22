using Domain.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.AI.Indexing.Embedding;

public class EmbeddingGeneratorFactory(IServiceProvider provider) : IEmbeddingGeneratorFactory
{
    private readonly IServiceProvider _provider = provider;

    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string modelName)
    {
        return _provider.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(modelName);
    }
}
