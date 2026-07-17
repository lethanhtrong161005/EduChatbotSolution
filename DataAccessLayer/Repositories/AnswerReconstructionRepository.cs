using DataAccess.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class AnswerReconstructionRepository(EduChatAiDbContext context)
{
    private readonly EduChatAiDbContext _context = context;

    public virtual Task<ChatMessage?> GetChatAssistantAsync(Guid assistantMessageId, CancellationToken cxlTkn = default) => _context.ChatMessages
        .Include(e => e.ChatSession).Include(e => e.GenerationSettings).Include(e => e.GenerationMetrics).Include(e => e.RequestMessages).Include(e => e.ResolvedSubjects)
        .Include(e => e.RetrievedContexts).Include(e => e.Citations).ThenInclude(e => e.CitationOccurrences).Include(e => e.Citations).ThenInclude(e => e.RetrievalSnapshot).Include(e => e.Citations).ThenInclude(e => e.Chunk).ThenInclude(e => e!.Document).ThenInclude(e => e.Subject)
        .AsNoTracking().AsSplitQuery().SingleOrDefaultAsync(e => e.Id == assistantMessageId && e.ChatRole == ChatRole.Assistant, cxlTkn);

    public virtual Task<TestResponse?> GetExperimentResponseAsync(Guid testResponseId, CancellationToken cxlTkn = default) => _context.TestResponses
        .Include(e => e.Experiment).ThenInclude(e => e.ConfigurationSnapshot).Include(e => e.RetrievedContexts).Include(e => e.RequestMessages).Include(e => e.EvaluationAttempts).ThenInclude(e => e.Metrics)
        .AsNoTracking().AsSplitQuery().SingleOrDefaultAsync(e => e.Id == testResponseId, cxlTkn);
}
