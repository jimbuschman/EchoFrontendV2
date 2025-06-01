using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EchoFrontendV2
{
    public class BackgroundFunctionCallManager : IDisposable
    {
        private readonly Channel<FunctionCallRequest> _functionQueue;
        private readonly ChannelWriter<FunctionCallRequest> _queueWriter;
        private readonly ChannelReader<FunctionCallRequest> _queueReader;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCalls;
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _backgroundProcessor;
        private readonly Func<ToolCall, string> _executeTool; // Injected delegate to your HandleToolCall

        private readonly int _maxRetries = 2;

        public BackgroundFunctionCallManager(Func<ToolCall, string> executeTool, int maxConcurrentCalls = 3)
        {
            _executeTool = executeTool;

            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _functionQueue = Channel.CreateBounded<FunctionCallRequest>(options);
            _queueWriter = _functionQueue.Writer;
            _queueReader = _functionQueue.Reader;

            _pendingCalls = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
            _executionSemaphore = new SemaphoreSlim(maxConcurrentCalls);
            _cancellationTokenSource = new CancellationTokenSource();

            _backgroundProcessor = Task.Run(ProcessFunctionCalls);
        }

        public async Task<string> QueueFunctionCallAsync(ToolCall toolCall, TimeSpan? timeout = null)
        {
            string id = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new FunctionCallRequest
            {
                Id = id,
                ToolCall = toolCall,
                CompletionSource = tcs,
                QueuedAt = DateTime.UtcNow
            };

            _pendingCalls[id] = tcs;
            await _queueWriter.WriteAsync(request);

            using var timeoutCts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _cancellationTokenSource.Token);

            try
            {
                return await tcs.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                _pendingCalls.TryRemove(id, out _);
                return $"{{\"error\": \"Function call timeout: {toolCall.Name}\"}}";
            }
        }

        private async Task ProcessFunctionCalls()
        {
            await foreach (var request in _queueReader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                _ = Task.Run(() => ExecuteToolCallAsync(request), _cancellationTokenSource.Token);
            }
        }

        private async Task ExecuteToolCallAsync(FunctionCallRequest request)
        {
            await _executionSemaphore.WaitAsync();
            try
            {
                string result = await ExecuteWithRetryAsync(request.ToolCall);
                request.CompletionSource.TrySetResult(result);
            }
            catch (Exception ex)
            {
                request.CompletionSource.TrySetResult($"{{\"error\": \"Execution failed: {ex.Message}\"}}");
            }
            finally
            {
                _pendingCalls.TryRemove(request.Id, out _);
                _executionSemaphore.Release();
            }
        }

        private async Task<string> ExecuteWithRetryAsync(ToolCall toolCall)
        {
            Exception lastException = null;
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));

                    return _executeTool(toolCall);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }
            throw lastException ?? new Exception("Function execution failed after retries.");
        }

        public void Dispose()
        {
            _queueWriter.Complete();
            _cancellationTokenSource.Cancel();
            _executionSemaphore.Dispose();
        }
    }

    public class FunctionCallRequest
    {
        public string Id { get; set; }
        public ToolCall ToolCall { get; set; }
        public TaskCompletionSource<string> CompletionSource { get; set; }
        public DateTime QueuedAt { get; set; }
    }

}
