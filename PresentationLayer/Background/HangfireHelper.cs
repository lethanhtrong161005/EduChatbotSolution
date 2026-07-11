using Hangfire;

namespace Presentation.Background;

public static class HangfireHelper
{
    public static void CancelJobs(Guid entityId, params string[] methodNames)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();

        var processingJobs = monitor.ProcessingJobs(0, int.MaxValue)
                                    .Where(x => methodNames.Contains(x.Value.Job.Method.Name));

        foreach (var job in processingJobs)
        {
            if (job.Value.Job.Args[0] is Guid id
                 && id == entityId)
            {
                BackgroundJob.Delete(job.Key);
            }
        }

        foreach (var queue in HangfireConstants.Queues)
        {
            var enqueuedJobs = monitor.EnqueuedJobs(queue, 0, int.MaxValue)
                                      .Where(x => methodNames.Contains(x.Value.Job.Method.Name));

            foreach (var job in enqueuedJobs)
            {
                if (job.Value.Job.Args[0] is Guid id
                     && id == entityId)
                {
                    BackgroundJob.Delete(job.Key);
                }
            }
        }

        var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue)
                                   .Where(x => methodNames.Contains(x.Value.Job.Method.Name));

        foreach (var job in scheduledJobs)
        {
            if (job.Value.Job.Args[0] is Guid id
                 && id == entityId)
            {
                BackgroundJob.Delete(job.Key);
            }
        }
    }
}
