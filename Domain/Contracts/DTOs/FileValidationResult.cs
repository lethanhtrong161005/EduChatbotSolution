using Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileValidationResult
{
    [MemberNotNullWhen(true, nameof(FileType))]
    public required bool Success { get; init; }

    public DocumentType? FileType { get; init; }

    public string[] Errors { get; init; } = [];
}
