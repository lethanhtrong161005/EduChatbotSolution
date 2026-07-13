namespace Domain.Contracts.DTOs;

public sealed record ChunkingOptions(int ChunkSize, int ChunkOverlap)
{
    public void Validate()
    {
        if (ChunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(ChunkSize), ChunkSize, "Chunk size must be positive.");
        if (ChunkOverlap < 0 || ChunkOverlap >= ChunkSize) throw new ArgumentOutOfRangeException(nameof(ChunkOverlap), ChunkOverlap, "Chunk overlap must be non-negative and smaller than chunk size.");
    }
}
