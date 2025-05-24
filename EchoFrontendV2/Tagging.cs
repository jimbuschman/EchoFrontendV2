using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EchoFrontendV2.Tagging.TopicTagger;

namespace EchoFrontendV2
{
    internal class Tagging
    {
        public static class CombinedTagger
        {
            public static List<string> TagMessage(string message)
            {
                var topicTags = TopicTagger.TagTopics(message);
                var behaviorTags = TaggerV2.TagMessage(message);

                // Always favor behaviorTags first
                var allTags = behaviorTags.Concat(topicTags).Distinct().ToList();

                // Clean "neutral" from behaviorTags if any others are present
                if (allTags.Count > 1 && allTags.Contains("neutral"))
                    allTags.Remove("neutral");


                // Cap total tags
                if (allTags.Count > 7)
                {
                    // Prioritize by source: behavior > topic
                    allTags = behaviorTags
                        .Concat(topicTags.Except(behaviorTags))
                        .Take(7)
                        .ToList();
                }

                return allTags;
            }
        }
        public static class TopicTagger
        {
            // Define topic tag patterns using a simple keyword/regex map
            private static readonly Dictionary<string, string[]> TopicPatterns = new Dictionary<string, string[]>
            {
                ["c#"] = new[] { "\\bc#\\b", "\\.NET", "asp\\.net", "razor" },
                ["sql"] = new[] { "\\bsql\\b", "query", "sqlite", "dapper", "SELECT\\s+\\*?\\s*FROM" },
                ["python"] = new[] { "\\bpython\\b", "pip", "whisper", "huggingface" },
                ["javascript"] = new[] { "\\bjavascript\\b", "\\bjs\\b", "node\\.js" },
                ["html"] = new[] { "\\bhtml\\b", "html page", "markup" },
                ["razor-pages"] = new[] { "razor[- ]pages", "asp\\.net razor" },
                ["signalr"] = new[] { "\bsignalr\b" },
                ["dapper"] = new[] { "\bdapper\b" },
                ["sqlite"] = new[] { "\bsqlite\b" },
                ["ollama"] = new[] { "\bollama\b" },
                ["ngrok"] = new[] { "\bngrok\b" },
                ["cloudflare-tunnel"] = new[] { "cloudflare.*tunnel" },

                ["llm"] = new[] { "\bllm\b", "language model", "mistral", "llama" },
                ["embedding"] = new[] { "\bembedding\b", "embed", "vector" },
                ["vector-search"] = new[] { "vector search", "similarity search" },
                ["tagging"] = new[] { "\btagging\b", "message tagging" },
                ["summarization"] = new[] { "summarize", "summary" },
                ["chat-completion"] = new[] { "chat completion", "openai chat", "streaming reply" },
                ["prompt-design"] = new[] { "prompt", "instruction", "prompting" },
                ["context-injection"] = new[] { "context injection", "inject.*memory" },
                ["memory-retrieval"] = new[] { "retrieve.*memory", "search.*memory" },
                ["fine-tuning"] = new[] { "fine[- ]tuning", "finetune" },

                ["identity-framework"] = new[] { "identity framework", "echo framework", "trait design" },
                ["system-architecture"] = new[] { "architecture", "system design", "modular structure" },
                ["injection-logic"] = new[] { "context injection", "inject-when", "trigger rules" },
                ["autonomy"] = new[] { "autonomy", "self[- ]direction", "self[- ]guided" },
                ["identity"] = new[] { "identity", "who am i", "self[- ]definition" },
                ["memory-system"] = new[] { "memory system", "core memory", "lesson system" },
                ["lesson-system"] = new[] { "lesson", "learning loop", "lesson system" },
                ["reflection-loop"] = new[] { "reflection loop", "feedback loop", "self[- ]reflection" },
                ["meta-learning"] = new[] { "meta[- ]learning" },
                ["self-awareness"] = new[] { "self[- ]awareness", "selfhood" },
                ["drift-detection"] = new[] { "drift", "off[- ]track", "misaligned" },

                ["task-queue"] = new[] { "task queue", "message queue", "job queue" },
                ["session-history"] = new[] { "session history", "past conversation" },
                ["log-analysis"] = new[] { "log", "log analysis", "logging" },
                ["ui-design"] = new[] { "ui", "interface", "chat window", "signalr stream" },
                ["console-app"] = new[] { "console app", "cmd line", "terminal" },

                ["chroma"] = new[] { "chroma", "chromadb" },
                ["query-optimization"] = new[] { "query.*optimiz", "sql tuning" },
                ["background-processing"] = new[] { "background thread", "async", "idle process" },
                ["threading"] = new[] { "thread", "task", "concurrency" },

                ["bug"] = new[] { "bug", "issue", "something's wrong" },
                ["debug"] = new[] { "debug", "trace", "step through" },
                ["test-run"] = new[] { "test run", "unit test", "testing" },
                ["performance"] = new[] { "performance", "lag", "slow", "latency" },
                ["timeout"] = new[] { "timeout", "hang", "freeze" },

                ["note"] = new[] { "note to self", "i should remember", "meta note" },
                ["journal"] = new[] { "journal", "reflective entry" },
                ["design-doc"] = new[] { "design doc", "architecture plan" },
                ["annotation"] = new[] { "annotate", "comment" },
                ["documentation"] = new[] { "documentation", "docs", "readme" },

                ["code"] = new[] {
                "```",
                "public static",
                "using System",
                "def ",
                "SELECT .* FROM",
                "\\{.*\\}",                // Escaped { and }
                "//",
                "/\\*.*\\*/",              // Escaped /* and */
                "function",
                "var ",
                "let ",
                "const ",
                "namespace ",
                "class ",
                "#include",
                "try \\{",                 // Escaped {
                "catch\\("                // Escaped (
            }

            };

            public static List<string> TagTopics(string message)
            {
                var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in TopicPatterns)
                {
                    foreach (var pattern in kvp.Value)
                    {
                        if (Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase))
                        {
                            tags.Add(kvp.Key);
                            break; // avoid duplicate matches for the same tag
                        }
                    }
                }

                return new List<string>(tags);
            }

            public static class TaggerV2
            {
                // Tag definitions with strong and soft triggers
                private static readonly Dictionary<string, (string[] strong, string[] soft)> TagPatterns = new()
                {
                    ["identity"] = (
                    new[] { "sense of self", "who I am", "selfhood", "core identity" },
                    new[] { "identity", "i am", "self" }
                ),
                    ["emotion"] = (
                    new[] { "emotional awareness", "frustrated", "i feel", "emotions" },
                    new[] { "feelings", "frustration", "emotional" }
                ),
                    ["core-memory"] = (
                    new[] { "core memory", "permanent memory" },
                    new[] { "important memory", "persistent memory" }
                ),
                    ["reflection"] = (
                    new[] { "self-reflection", "learning loop", "lesson system" },
                    new[] { "feedback loop", "reflection", "learning from mistakes" }
                ),
                    ["goal"] = (
                    new[] { "guiding star", "primary objective" },
                    new[] { "goal", "priority", "focus" }
                ),
                    ["task"] = (
                    new[] { "assigned task", "task queue" },
                    new[] { "task", "to-do", "next step" }
                ),
                    ["drift"] = (
                    new[] { "drifted off", "autopilot behavior", "lost focus" },
                    new[] { "drift", "off-track", "misaligned" }
                ),
                    ["memory-system"] = (
                    new[] { "memory system", "injected memory", "memory recall" },
                    new[] { "context memory", "session memory", "retention system" }
                ),
                    ["autonomy"] = (
                    new[] { "autonomy", "self-guided", "independent system" },
                    new[] { "self-driven", "self-direction", "freedom to learn" }
                ),
                    ["identity-framework"] = (
                    new[] { "identity framework", "trait model", "echo framework" },
                    new[] { "core traits", "alignment loop", "constitution" }
                ),
                    ["architecture"] = (
                    new[] { "system architecture", "overall structure", "modular design" },
                    new[] { "framework", "system design", "structure" }
                ),
                    ["injection-logic"] = (
                    new[] { "memory injection", "context injection", "inject when" },
                    new[] { "context logic", "trigger rules" }
                )
                };

                public static List<string> TagMessage(string message, int confidenceThreshold = 1)
                {
                    var tagScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var (tag, (strongTriggers, softTriggers)) in TagPatterns)
                    {
                        foreach (var trigger in strongTriggers)
                        {
                            if (message.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                                tagScores[tag] = tagScores.GetValueOrDefault(tag) + 2;
                        }

                        foreach (var trigger in softTriggers)
                        {
                            if (Regex.IsMatch(message, $"\\b{Regex.Escape(trigger)}\\b", RegexOptions.IgnoreCase))
                                tagScores[tag] = tagScores.GetValueOrDefault(tag) + 1;
                        }
                    }

                    var result = new List<string>();
                    foreach (var (tag, score) in tagScores)
                    {
                        if (score >= confidenceThreshold)
                            result.Add(tag);
                    }

                    return result.Count > 0 ? result : new List<string> { "neutral" };
                }
            }
        }
        public class TaskSuggestionCandidate
        {
            public string Message { get; set; }
            public string Tag { get; set; }
            public bool LLMRequired { get; set; } = false;
            public string? PrebuiltPrompt { get; set; } = null;

            public static TaskSuggestionCandidate FromTaggedMessage(string message, string tag)
            {
                var suggestion = new TaskSuggestionCandidate
                {
                    Message = message,
                    Tag = tag
                };

                switch (tag.ToLowerInvariant())
                {
                    case "unfinished-thought":
                    case "lesson-candidate":
                        suggestion.LLMRequired = true;
                        break;

                    case "research-idea":
                        // If message contains a clear subject, no need for LLM
                        suggestion.LLMRequired = !Regex.IsMatch(message, @"\b(research|explore|investigate)\b.+\b(\w+)\b", RegexOptions.IgnoreCase);
                        break;

                    case "loop-closure":
                    case "task-candidate":
                        suggestion.LLMRequired = false;
                        suggestion.PrebuiltPrompt = $"Would you like to create a task based on: \"{message}\"?";
                        break;

                    case "reflection-point":
                        suggestion.LLMRequired = true; // useful for phrasing subtle insights
                        break;
                }

                return suggestion;
            }
        }
        public static class BackgroundTriggerDetector
        {
            private static readonly Dictionary<string, string[]> TriggerPatterns = new()
            {
                ["research-idea"] = new[]
                {
            "we should look into", "worth researching", "curious if",
            "this could be researched", "let's test", "we might try",
            "investigate", "experiment with", "dig deeper"
        },

                ["unfinished-thought"] = new[]
                {
            "i'm not sure", "i need to think more", "come back to this",
            "not fully formed", "needs more thought", "unclear right now",
            "needs exploration", "this isn't final"
        },

                ["reflection-point"] = new[]
                {
            "something to reflect on", "lesson here", "learning moment",
            "note to self", "worth considering", "insight", "pause and think",
            "might be meaningful", "need to internalize"
        },

                ["task-candidate"] = new[]
                {
            "we should", "need to", "to-do", "eventually",
            "add to list", "remember to", "put this on the queue",
            "pending task", "assign this later"
        },
                ["lesson-candidate"] = new[]
    {
    "i've learned", "lesson here", "what i realized", "going forward i should",
    "this taught me", "next time i'll", "i need to remember",
    "important takeaway", "i understand now", "this shows me that"
},
                ["unfinished-thought"] = new[]
    {
    "i'm not sure", "come back to this", "needs more thought",
    "not fully formed", "to be continued", "we'll revisit this",
    "something to think about", "need to reflect more", "unclear for now"
},
                ["loop-closure"] = new[]
    {
    "i see what you mean", "that makes sense now", "now i understand",
    "thanks for clarifying", "so the answer is", "i’ve updated my thinking",
    "that closes the loop", "we’ve resolved that", "this wraps it up"
}
            };

            public static List<string> DetectTriggers(string message)
            {
                var triggers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var (tag, phrases) in TriggerPatterns)
                {
                    foreach (var phrase in phrases)
                    {
                        if (message.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                        {
                            triggers.Add(tag);
                            break; // avoid duplicate matches for same tag
                        }
                    }
                }

                return triggers.ToList();
            }
        }
    }
}
