using Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileReceptionResult
{
    [MemberNotNullWhen(true, nameof(StagingLocator), nameof(FileType))]
    public required bool Success { get; init; }

    public string? StagingLocator { get; init; }

    public DocumentType? FileType { get; init; }

    public string[] Errors { get; init; } = [];
}
