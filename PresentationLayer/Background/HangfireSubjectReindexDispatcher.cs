using Domain.Contracts;
using Domain.Contracts.DTOs;
using Hangfire;

namespace Presentation.Background;

public sealed class HangfireSubjectReindexDispatcher : ISubjectReindexDispatcher
{
    public string Enqueue(int subjectId, EffectiveAiConfiguration configuration) =>
        BackgroundJob.Enqueue<SubjectReindexJob>(
            job => job.RunAsync(subjectId, configuration, CancellationToken.None));
}
