using Domain.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileValidationResult
{
    [MemberNotNullWhen(true, nameof(FileType))]
    public required bool Success { get; init; }

    public FileType? FileType { get; init; }

    public string[] Errors { get; init; } = [];
}
