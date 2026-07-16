using Domain.Common;
using Domain.Contracts;
using Hangfire;
namespace Presentation.Background;

public sealed class UserImportJob(IUserImportCoordinator coordinator)
{
    [Retry(Retries = 2)]
    [Queue(HangfireConstants.MediumPriorityQueue)]
    public async Task ImportAsync(Guid batchId, CancellationToken cxlTkn = default) =>
        await coordinator.ImportAsync(batchId, cxlTkn);
}
