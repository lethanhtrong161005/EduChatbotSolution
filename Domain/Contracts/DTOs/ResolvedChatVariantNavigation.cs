using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record ResolvedChatVariantOption
{
    public required Guid MessageId { get; init; }
    public required int VariantIndex { get; init; }
    public required bool IsSelected { get; init; }
    public required MessageStatus Status { get; init; }
}

public record ResolvedChatVariantNavigation
{
    public required Guid UserMessageId { get; init; }
    public required int CurrentVariantIndex { get; init; }
    public required int TotalVariantCount { get; init; }
    public required IReadOnlyList<ResolvedChatVariantOption> Variants { get; init; }
}
