using Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Contracts.DTOs;

public record FileInspectionResult
{
    [MemberNotNullWhen(true, nameof(FilePath), nameof(FileType))]
    public required bool Success { get; set; }

    public string? FilePath { get; set; }

    public DocumentType? FileType { get; set; }

    public string[] Errors { get; set; } = [];
}
