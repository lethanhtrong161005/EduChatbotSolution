using Hangfire;

namespace Business.Background;

public static class HangfireHelper
{
    public static void CancelJobs(Guid entityId, params string[] methodNames)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();

        var jobsProcessing = monitor.ProcessingJobs(0, int.MaxValue)
                                    .Where(x => methodNames.Contains(x.Value.Job.Method.Name));

        foreach (var j in jobsProcessing)
        {
            if (j.Value.Job.Args[0] is Guid id
                 && id == entityId)
            {
                BackgroundJob.Delete(j.Key);
            }
        }

        var jobsScheduled = monitor.ScheduledJobs(0, int.MaxValue)
                                   .Where(x => methodNames.Contains(x.Value.Job.Method.Name));

        foreach (var j in jobsScheduled)
        {
            if (j.Value.Job.Args[0] is Guid id
                 && id == entityId)
            {
                BackgroundJob.Delete(j.Key);
            }
        }
    }
}
