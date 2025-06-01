using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace EchoFrontendV2
{
    public class Tool
    {
        public string name { get; set; }
        public string description { get; set; }
        public Dictionary<string, ToolParam> parameters { get; set; }
    }

    public class ToolParam
    {
        public string type { get; set; }
        public string description { get; set; }
    }
    public class ToolCall
    {
        [JsonPropertyName("tool_name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments")]
        public Dictionary<string, object> Arguments { get; set; }
    }

    public class ToolOutput
    {
        [JsonPropertyName("tool_output")]
        public ToolOutputPayload Payload { get; set; }
    }

    public class ToolOutputPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }
    }
}
