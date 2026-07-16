using Domain.Common;
using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public class UserImportFilePersistenceJob(IUserImportFileService fileService)
{
    [Retry(Retries = 2)]
    [Queue(HangfireConstants.MediumPriorityQueue)]
    public async Task PersistAsync(Guid batchId, CancellationToken cxlTkn = default)
    {
        var result = await fileService.PersistAsync(batchId, cxlTkn);
        if (!result.Success) throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
    }
}
