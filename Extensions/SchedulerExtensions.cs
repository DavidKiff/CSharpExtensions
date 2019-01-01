using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Extensions
{
    public static class SchedulerExtensions
    {
        public static Task<T> Schedule<T>(this IScheduler scheduler, Func<T> work)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            scheduler.Schedule(() =>
            {
                try
                {
                    taskCompletionSource.SetResult(work());
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            return taskCompletionSource.Task;
        }
    }
}
