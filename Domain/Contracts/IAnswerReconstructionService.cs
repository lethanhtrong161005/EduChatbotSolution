using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IAnswerReconstructionService
{
    Task<ChatAnswerReconstructionDto?> ReconstructChatAssistantAsync(Guid assistantMessageId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ExperimentAnswerReconstructionDto?> ReconstructExperimentResponseAsync(Guid testResponseId, CancellationToken cancellationToken = default);
}
