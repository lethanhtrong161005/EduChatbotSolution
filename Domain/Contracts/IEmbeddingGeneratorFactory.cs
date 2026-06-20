using Microsoft.Extensions.AI;

namespace Domain.Contracts;

public interface IEmbeddingGeneratorFactory
{
    IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string modelName);
}
