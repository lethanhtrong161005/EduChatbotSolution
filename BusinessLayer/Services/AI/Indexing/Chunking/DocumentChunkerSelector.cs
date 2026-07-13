using Domain.Contracts;

namespace Business.Services.AI.Indexing.Chunking;

public sealed class DocumentChunkerSelector : IDocumentChunkerSelector
{
    private readonly Dictionary<string, IDocumentChunker> _chunkers = new(StringComparer.Ordinal);

    public DocumentChunkerSelector(IEnumerable<IDocumentChunker> chunkers)
    {
        ArgumentNullException.ThrowIfNull(chunkers);

        foreach (var chunker in chunkers)
        {
            if (string.IsNullOrWhiteSpace(chunker.StrategyName)) throw new InvalidOperationException($"Chunker '{chunker.GetType().Name}' has no strategy name.");
            if (!_chunkers.TryAdd(chunker.StrategyName, chunker)) throw new InvalidOperationException($"Multiple chunkers are registered for strategy '{chunker.StrategyName}'.");
        }

        if (_chunkers.Count == 0) throw new InvalidOperationException("No document chunkers are registered.");
    }

    public IDocumentChunker Select(string strategy)
    {
        if (string.IsNullOrWhiteSpace(strategy)) throw new ArgumentException("Chunking strategy is required.", nameof(strategy));
        return _chunkers.TryGetValue(strategy, out var chunker) ? chunker : throw new InvalidOperationException($"No document chunker is registered for strategy '{strategy}'.");
    }
}
