using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface ISubjectReindexDispatcher
{
    string Enqueue(int subjectId, EffectiveAiConfiguration configuration);
}
