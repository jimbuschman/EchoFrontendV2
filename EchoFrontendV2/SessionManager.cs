using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestSQLLite
{
    public class SessionMessage
    {

        [JsonPropertyName("role")]
        public string Role { get; set; } // "user", "assistant", "system"
        [JsonPropertyName("content")]
        public string Content { get; set; }
        [JsonIgnore]
        public DateTime TimeStamp { get; set; }
        [JsonIgnore]
        public bool Dumped { get; set; } = false; // Flag to indicate if the message has been dumped to memory
        [JsonIgnore]
        public List<string> Images { get; set; }
        //[JsonIgnore]
        //public string ToolCallId { get; set; }
        [JsonPropertyName("tool_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ToolName { get; set; }
        public SessionMessage(string role, string content, bool isOldMessage = false)
        {
            Role = role;
            Content = content;            
            TimeStamp = DateTime.UtcNow;
            Dumped = isOldMessage;
        }
    }

    public class SessionManager
    {
        public int SessionId { get; set; }        

        private readonly Dictionary<int, List<SessionMessage>> _sessions = new();

        public void AddMessage(SessionMessage message)
        {
            if (!_sessions.ContainsKey(SessionId))
            {
                _sessions[SessionId] = new List<SessionMessage>();
            }
            message.Content = CleanText(message.Content); // Clean the text
            _sessions[SessionId].Add(message);
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var cleaned = input
                .Trim()
                .Replace("\r\n", "\n") // Normalize newlines
                .Replace("\t", " ")     // Remove tabs
                .Replace("  ", " ");    // Collapse double spaces

            cleaned = Regex.Replace(cleaned, @"[^\u0000-\u007F]+", string.Empty); // Remove non-ASCII if needed

            return cleaned;
        }     
        public List<SessionMessage> GetCurrentSessionMessages()
        {
            return _sessions.TryGetValue(SessionId, out var messages)
                ? messages
                : new List<SessionMessage>();
        }
    }
}
