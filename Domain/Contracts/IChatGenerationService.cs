using Domain.DTOs;

namespace Domain.Contracts;

public interface IChatGenerationService
{
    Task<ChatGenerationResult> GenerateAsync(
        ChatGenerationRequest request,
        Func<string, Task> onToken,
        CancellationToken cancellationToken = default);
}
