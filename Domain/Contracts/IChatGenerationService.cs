using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IChatGenerationService
{
    Task<TitleGenerationResult> GenerateTitleAsync(
        TitleGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<ChatGenerationResult> GenerateChatAsync(
        ChatGenerationRequest request,
        Func<string, Task> onToken,
        CancellationToken cancellationToken = default);
}
