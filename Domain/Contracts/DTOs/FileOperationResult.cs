using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileOperationResult
{
    [MemberNotNullWhen(true, nameof(FilePath))]
    public required bool Success { get; set; }

    public string? FilePath { get; set; }

    public string[] Errors { get; set; } = [];
}
