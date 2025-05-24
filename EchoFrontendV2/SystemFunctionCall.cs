using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestSQLLite
{
    public class SystemFunctionCall
    {
        public string Type { get; set; }         // e.g., FRAMEWORK_MEMORY_ENTRY, TRAIT_UPDATE, etc.
        public string RawContent { get; set; }   // The full content inside the <FUNCTION> block
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public static List<SystemFunctionCall> Parse(string fullText)
        {
            var matches = Regex.Matches(fullText, @"<FUNCTION>(.*?)</FUNCTION>", RegexOptions.Singleline);
            var results = new List<SystemFunctionCall>();

            foreach (Match match in matches)
            {
                string block = match.Groups[1].Value.Trim();

                // Extract the function type: # [TYPE]
                var headerMatch = Regex.Match(block, @"^\s*#\s*\[(.+?)\]", RegexOptions.Multiline);
                if (!headerMatch.Success)
                    continue;

                string type = headerMatch.Groups[1].Value.Trim().ToUpperInvariant();

                if(type.Trim().ToUpper() == "FRAMEWORK_MEMORY_ENTRY")
                {
                    results.Add(CoreMemoryFunction.ParseCoreMemory(block));
                }
            }

            return results;
        }

        protected static string ExtractField(string input, string fieldLabel)
        {
            var match = Regex.Match(input, $@"{Regex.Escape(fieldLabel)}\s*(.+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        protected static List<string> ExtractList(string input, string fieldLabel)
        {
            var raw = ExtractField(input, fieldLabel);
            return string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        }

        protected static string ExtractMultilineField(string input, string fieldLabel)
        {
            // Handles: Content: """...multiline..."""
            string pattern = @"Content:\s*""""""([\s\S]*?)""""""";
            var match = Regex.Match(input, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                //Console.WriteLine("❌ No match found.");
                return null;
            }

            return match.Groups[1].Value.Trim();
        }

    }

    public class CoreMemoryFunction : SystemFunctionCall
    {
        public string Name { get; set; }
        public string EntryType { get; set; } // e.g., framework, identity
        public List<string> Tags { get; set; }
        public List<string> InjectWhen { get; set; }
        public string Content { get; set; }

        public static CoreMemoryFunction ParseCoreMemory(string rawBlock)
        {
            var entry = new CoreMemoryFunction
            {
                Type = "FRAMEWORK_MEMORY_ENTRY",
                RawContent = rawBlock
            };

            entry.Name = ExtractField(rawBlock, "Name:");
            entry.EntryType = ExtractField(rawBlock, "Type:");
            entry.Tags = ExtractList(rawBlock, "Tags:");
            entry.InjectWhen = ExtractList(rawBlock, "InjectWhen:");
            entry.Content = ExtractMultilineField(rawBlock, "Content:");

            return entry;
        }
    }
}
