using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileReadResult
{
    [MemberNotNullWhen(true, nameof(FileStream))]
    public required bool Success { get; init; }

    public Stream? FileStream { get; init; }

    public string[] Errors { get; init; } = [];
}
