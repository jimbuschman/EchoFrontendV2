using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EchoFrontendV2.DTO
{
    public class CoreMemory
    {
        public int Id { get; set; }                     // Optional, if stored in DB
        public string Name { get; set; }                // e.g., "Emotional Framework"
        public string Type { get; set; }                // e.g., "framework", "identity", "function_manifest"
        public string Content { get; set; }             // The actual memory (Markdown, JSON, or natural language)

        public List<string> Tags { get; set; } = new(); // Used for context matching (e.g., ["emotion", "relationship"])
        public List<string> InjectWhen { get; set; } = new(); // e.g., ["session_start", "topic_match"]

        public bool IsActive { get; set; } = true;      // Marked false if deprecated or replaced
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }

        // Optional metadata
        public string Source { get; set; }              // e.g., "manual", "lesson", "system_default"
        public double Priority { get; set; } = 1.0;      // Used for injection order/scoring
        public string Notes { get; set; }               // Optional internal notes
        public CoreMemoryBlob Blob { get; set; } = new();       
    }
}
