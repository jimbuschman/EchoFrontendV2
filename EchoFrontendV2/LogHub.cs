using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EchoFrontendV2
{
    /// <summary>
    /// Delegate for log message events
    /// </summary>
    public delegate void LogMessageEventHandler(string message, string level);

    /// <summary>
    /// A replacement for SignalR's LogHub that works in WinForms
    /// </summary>
    public class LogHub
    {
        // Singleton pattern to mimic the global nature of SignalR hubs
        private static LogHub _instance;
        public static LogHub Instance => _instance ??= new LogHub();

        // Event that UI components can subscribe to
        public event LogMessageEventHandler LogReceived;

        private LogHub() { }

        // Method that replaces SendAsync
        public void SendLog(string message, string level)
        {
            // Invoke the event on the UI thread if we have subscribers
            LogReceived?.Invoke(message, level);
        }
    }

    /// <summary>
    /// WinForms implementation of RealtimeLogger 
    /// </summary>
    public class RealtimeLogger
    {
        private readonly LogHub _logHub;

        // Constructor that accepts a LogHub reference for unit testing
        public RealtimeLogger(LogHub logHub = null)
        {
            _logHub = logHub ?? LogHub.Instance;
        }

        // All the original methods preserved with the same signature
        public void Log(string message)
        {
            // You could also write to file or database here
            _logHub.SendLog(message, "info");
        }

        public void LogMessage(string message)
        {
            // You could also write to file or database here
            _logHub.SendLog(message, "tracking");
        }

        public void LogWarning(string message)
        {
            // You could also write to file or database here
            _logHub.SendLog(message, "warn");
        }

        public void LogException(string message)
        {
            // You could also write to file or database here
            _logHub.SendLog(message, "error");
        }
    }

    /// <summary>
    /// Example UI component that displays logs
    /// </summary>
    public class LogViewer
    {
        private readonly ListBox _logListBox;
        private readonly int _maxLogs;

        public LogViewer(ListBox logListBox, int maxLogs = 1000)
        {
            _logListBox = logListBox;
            _maxLogs = maxLogs;

            // Subscribe to the log events
            LogHub.Instance.LogReceived += OnLogReceived;
        }

        private void OnLogReceived(string message, string level)
        {
            // Make sure we update the UI on the UI thread
            if (_logListBox.InvokeRequired)
            {
                _logListBox.Invoke(new Action<string, string>(OnLogReceived), message, level);
                return;
            }

            // Format the log entry based on level
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level.ToUpper()}] {message}";

            // Add the new log entry
            _logListBox.Items.Add(logEntry);

            // Trim old logs if we exceed the maximum
            while (_logListBox.Items.Count > _maxLogs)
            {
                _logListBox.Items.RemoveAt(0);
            }

            // Auto-scroll to the bottom
            _logListBox.TopIndex = _logListBox.Items.Count - 1;
        }

        // Cleanup method to unsubscribe from events
        public void Dispose()
        {
            LogHub.Instance.LogReceived -= OnLogReceived;
        }
    }
}