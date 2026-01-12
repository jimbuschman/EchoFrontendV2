using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2
{
    public class EndpointManager
    {
        private readonly List<LLMEndpoint> _endpoints;
        private readonly object _endpointLock = new();
        private LLMEndpoint _currentEndpoint;
        private readonly RealtimeLogger _logger;

        public EndpointManager(RealtimeLogger logger)
        {
            _logger = logger;
            _endpoints = new List<LLMEndpoint>
            {
                new LLMEndpoint("Primary", "http://localhost:11434/", 10, true),
                new LLMEndpoint("Secondary", "https://0272-2604-2d80-a602-400-9d22-1ee2-d29b-28ef.ngrok-free.app", 5, true)
            };

            SelectBestEndpoint();
        }

        public LLMEndpoint GetCurrentEndpoint()
        {
            lock (_endpointLock)
            {
                return _currentEndpoint;
            }
        }

        public void SelectBestEndpoint()
        {
            lock (_endpointLock)
            {
                _currentEndpoint = _endpoints
                    .Where(e => e.IsEnabled)
                    .OrderByDescending(e => e.Priority)
                    .ThenByDescending(e => e.LastResponseTime)
                    .FirstOrDefault();

                if (_currentEndpoint == null)
                {
                    throw new InvalidOperationException("No available LLM endpoints");
                }

                _logger.LogMessage($"Selected endpoint: {_currentEndpoint.Name} ({_currentEndpoint.BaseUrl})");
            }
        }

        public void MarkEndpointAsFailed(LLMEndpoint endpoint)
        {
            lock (_endpointLock)
            {
                endpoint.IsEnabled = false;
                _logger.LogMessage($"Endpoint marked as failed: {endpoint.Name}");

                if (endpoint == _currentEndpoint)
                {
                    SelectBestEndpoint();
                }
            }
        }

        public void UpdateEndpointPerformance(LLMEndpoint endpoint, TimeSpan responseTime, bool success)
        {
            lock (_endpointLock)
            {
                endpoint.LastResponseTime = responseTime;
                endpoint.LastUsed = DateTime.UtcNow;

                if (!success)
                {
                    endpoint.FailureCount++;
                    if (endpoint.FailureCount > 3)
                    {
                        endpoint.IsEnabled = false;
                    }
                }
                else
                {
                    endpoint.FailureCount = 0;
                    endpoint.SuccessCount++;
                }
            }
        }
        public LLMEndpoint GetAvailableEndpoint()
        {
            lock (_endpointLock)
            {
                return _endpoints
                    .Where(e => e.CanAcceptRequest)
                    .OrderByDescending(e => e.Priority)
                    .ThenBy(e => e.ActiveRequests) // Prefer less busy endpoints
                    .FirstOrDefault();
            }
        }
    }

    public class LLMEndpoint
    {
        public string Name { get; }
        public string BaseUrl { get; }
        public int Priority { get; }
        public bool IsEnabled { get; set; }
        public DateTime LastUsed { get; set; }
        public TimeSpan LastResponseTime { get; set; }
        public int FailureCount { get; set; }
        public int SuccessCount { get; set; }
        private int _activeRequests;
        public int ActiveRequests => _activeRequests; // Read-only property
        public int MaxConcurrentRequests { get; set; } = 1; // Configurable

        public bool CanAcceptRequest =>
            IsEnabled && ActiveRequests < MaxConcurrentRequests;

        public void StartRequest() => Interlocked.Increment(ref _activeRequests);
        public void CompleteRequest() => Interlocked.Decrement(ref _activeRequests);

        public LLMEndpoint(string name, string baseUrl, int priority, bool isEnabled)
        {
            Name = name;
            BaseUrl = baseUrl;
            Priority = priority;
            IsEnabled = isEnabled;
            LastUsed = DateTime.UtcNow;
            LastResponseTime = TimeSpan.FromSeconds(1);
            FailureCount = 0;
            SuccessCount = 0;
        }
    }
}
