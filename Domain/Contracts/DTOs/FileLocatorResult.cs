using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileLocatorResult
{
    [MemberNotNullWhen(true, nameof(Locator))]
    public required bool Success { get; init; }

    public string? Locator { get; init; }

    public string[] Errors { get; init; } = [];
}
