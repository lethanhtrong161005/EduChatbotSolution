using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IAiConfigurationAdminService
{
    Task<IReadOnlyList<AiConfigurationSubjectOptionDto>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<AiConfigurationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<SubjectAiConfigurationDto?> GetSubjectConfigurationAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<SubjectAiConfigurationDto> SaveSubjectConfigurationAsync(int subjectId, SaveSubjectAiConfigurationRequest request, CancellationToken cancellationToken = default);
    Task<SubjectReindexResponseDto> ReindexSubjectAsync(int subjectId, CancellationToken cancellationToken = default);
}
