using Domain.Common;
using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public sealed class DocumentPersistenceJob(IDocumentFileService fileService)
{
    [Retry(Retries = 4)]
    [Queue(HangfireConstants.LowPriorityQueue)]
    public async Task PersistAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var result = await fileService.PersistAsync(documentId, cxlTkn);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
    }
}
