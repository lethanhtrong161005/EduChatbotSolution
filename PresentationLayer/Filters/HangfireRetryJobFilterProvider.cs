using Domain.Common;
using Hangfire;
using Hangfire.Common;
using System.Reflection;

namespace Presentation.Filters;

public class HangfireRetryJobFilterProvider : IJobFilterProvider
{

    public IEnumerable<JobFilter> GetFilters(Job job)
    {
        var retryAttr = job.Method.GetCustomAttribute<RetryAttribute>();

        return retryAttr != null
            ? [
            new JobFilter(
                new AutomaticRetryAttribute { Attempts = retryAttr.Retries },
                JobFilterScope.Method,
                null),
            ]
            : [];
    }
}
