using Domain.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileReceptionResult
{
    [MemberNotNullWhen(true, nameof(Locator), nameof(FileType))]
    public required bool Success { get; init; }

    public string? Locator { get; init; }

    public FileType? FileType { get; init; }

    public string[] Errors { get; init; } = [];
}
