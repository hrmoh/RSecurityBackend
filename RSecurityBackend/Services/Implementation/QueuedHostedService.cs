using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// BackgroundService implementation
    /// </summary>
    public class QueuedHostedService : BackgroundService
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="taskQueue"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger, IConfiguration configuration)
        {
            TaskQueue = taskQueue;
            _logger = logger;

            // Was always strictly sequential (one item at a time) before this change, regardless
            // of how many items were queued — this is why running multiple IIS worker processes
            // felt like it enabled real parallelism: two jobs could only ever run at the same
            // time if IIS happened to route their triggers to two DIFFERENT processes, which
            // isn't something you can rely on or control. Defaults to 1 (unchanged behavior) so
            // this is opt-in, not a silent behavior change — see the appsettings.json note below
            // before raising it: not every existing background job in this codebase was written
            // assuming another job (of the same or a different kind) could be running at the same
            // time. Jobs that already guard against overlapping with THEMSELVES (e.g. the public
            // data export's TryStartExclusiveExportJob) are fine either way; jobs that don't have
            // such a guard, or that share an external resource (a file, a git working copy, etc.)
            // with another job type, are the ones worth checking before turning this up.
            _maxConcurrency = configuration.GetValue<int?>("BackgroundTaskQueue:MaxConcurrency") ?? 1;
            if (_maxConcurrency < 1)
                _maxConcurrency = 1;
        }

        private readonly ILogger<QueuedHostedService> _logger;
        private readonly int _maxConcurrency;

        /// <summary>
        /// task queue
        /// </summary>
        public IBackgroundTaskQueue TaskQueue { get; }

        /// <summary>
        /// execute
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            // BackgroundTaskQueue's DequeueAsync (SemaphoreSlim + ConcurrentQueue) is already
            // safe for multiple concurrent consumers pulling from the same queue - no change
            // needed there. Running _maxConcurrency of these loops side by side is what actually
            // gives you real, deliberate parallelism, instead of the IIS-routing-luck version.
            var workers = new Task[_maxConcurrency];
            for (int i = 0; i < _maxConcurrency; i++)
            {
                int workerId = i;
                workers[i] = RunWorkerLoopAsync(workerId, cancellationToken);
            }
            await Task.WhenAll(workers);
        }

        private async Task RunWorkerLoopAsync(int workerId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Func<CancellationToken, Task> workItem;
                try
                {
                    workItem = await TaskQueue.DequeueAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break; // normal shutdown, not an error
                }

                if (workItem == null)
                    continue;

                try
                {
                    await workItem(cancellationToken);
                }
                catch (Exception exp)
                {
                    // previously: catch { } - silently swallowed every exception from every
                    // background job, with no trace anywhere that it happened. Now at least
                    // logged, so "a job seemed to just stop" is diagnosable.
                    _logger.LogError(exp, "Background work item (worker {WorkerId}) threw an unhandled exception.", workerId);
                }
            }
        }
    }
}
