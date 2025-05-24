namespace EchoFrontendV2
{
    using System.Collections.Concurrent;

    public class LLMJobQueue
    {
        private readonly SortedDictionary<int, Queue<LLMJob>> _priorityBuckets = new();
        private readonly SemaphoreSlim _queueSignal = new(0);
        private readonly object _lock = new();
        private readonly RealtimeLogger _logger;
        public LLMJobQueue(RealtimeLogger logger)
        {
            _logger = logger;
            Task.Run(ProcessQueue);
        }

        public Task<T> EnqueueAndWait<T>(Func<Task<T>> job, int priority = 10)
        {
            var tcs = new TaskCompletionSource<T>();

            var llmJob = new LLMJob
            {
                Priority = priority,
                Job = async () =>
                {
                    try
                    {
                        var result = await job();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            };

            lock (_lock)
            {
                if (!_priorityBuckets.TryGetValue(priority, out var bucket))
                {
                    bucket = new Queue<LLMJob>();
                    _priorityBuckets[priority] = bucket;
                }
                bucket.Enqueue(llmJob);
            }

            _queueSignal.Release();
            return tcs.Task;
        }

        private async Task ProcessQueue()
        {
            while (true)
            {
               
                await _queueSignal.WaitAsync();

                LLMJob nextJob = null;
                lock (_lock)
                {
                    foreach (var bucket in _priorityBuckets.OrderBy(b => b.Key))
                    {
                        _logger.LogWarning("Priority: "+ bucket.Key+" Queue Count: " + bucket.Value.Count);
                        if (bucket.Value.Count > 0)
                        {
                            nextJob = bucket.Value.Dequeue();
                            break;
                        }
                    }
                }

                if (nextJob != null)
                {
                    try
                    {
                        await nextJob.Job();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException("LLMJobQueue:ProcessQueue(): " + ex.ToString());                        
                    }
                }
            }
        }
    }

    public class LLMJob
    {
        public Func<Task> Job { get; set; }
        public int Priority { get; set; } // Lower number = higher priority
    }


}
