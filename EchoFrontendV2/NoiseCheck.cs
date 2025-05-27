using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EchoFrontendV2
{
    public static class NoiseCheck
    {
        private static readonly string[] SignalWords = new[]
            {
                "you", "feel", "doing", "okay", "sure", "think", "remember",
                "what", "why", "still", "about", "mean", "want", "been"
            };

        public static bool ContainsSignalWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string lowerInput = input.ToLowerInvariant();
            return SignalWords.Any(word => lowerInput.Contains(word));
        }
        private static readonly HashSet<string> NoisePhrases = new(StringComparer.OrdinalIgnoreCase)
        {
        // Greetings
        "hi", "hello", "hey", "hi there", "hey there", "yo",
        "what's up", "sup", "howdy", "good morning", "good afternoon", "good night",
        "bye", "goodbye", "see ya", "later", "take care", "gn", "night",

        // Short affirmatives/negatives
        "ok", "okay", "yeah", "nah", "maybe", "got it", "roger", "sure", "yup", "nope",
        "yes", "no", "alright", "right", "uh-huh", "mm-hmm", "mhm", "aye", "bet", "fine", "k", "kk",

        // Generic reactions
        "wow", "oh", "ah", "huh", "oops", "whoops", "hm", "hmm", "heh", "huh", "hmm ok", "okay then",
        "cool", "nice", "great", "awesome", "interesting", "noted", "makes sense", "understood",

        // Internet/text slang
        "lol", "haha", "lmao", "lmfao", "rofl", "smh", "brb", "btw", "idk", "imo", "imho",
        "tbh", "omg", "omfg", "ikr", "yeet", "fr", "nvm", "ffs", "wtf", "wth",

        ":)", ":(", ":D", "👍", "👀", "💀", "💯", "😂", "😭", "👌", "✌️", "❤️", "?", "??", "…", "...."
        };

        // Phrases considered filler *only when used alone*
        private static readonly HashSet<string> FillerWords = new(StringComparer.OrdinalIgnoreCase)
        {
        "um", "uh", "well", "like", "you know", "i mean"
        };

        public static bool Check(string text, int length = 80)
        {
            return (text.Length < length && !IsNoise(text) && IsDeclarative(text));
        }
        private static bool IsNoise(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            string normalized = NormalizeText(text); //text.Trim().ToLowerInvariant();

            // Match exact known noise phrases
            if (NoisePhrases.Contains(normalized))
                return true;

            // Check for short filler words alone
            if (FillerWords.Contains(normalized))
                return true;

            // Very short message like "ok." or "yes!" (under 6 chars, alpha + punctuation)
            if (normalized.Split(' ').Length <= 2 && normalized.Length <= 10)
                return true;

            // Laughter or slang patterns
            if (Regex.IsMatch(normalized, @"^(ha|lol|lmao|rofl)+[!]*$", RegexOptions.IgnoreCase))
                return true;

            return false;
        }
        private static string NormalizeText(string input)
        {
            string lower = input.ToLowerInvariant().Trim();
            lower = Regex.Replace(lower, @"[^\p{L}\p{N}\s]", "");
            lower = Regex.Replace(lower, @"[^\w\s]", ""); // remove punctuation
            lower = Regex.Replace(lower, @"\s+", " ");    // normalize whitespace
            return lower;
        }
        private static bool IsDeclarative(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            // Check it starts with "I", "My", or common declarative structures like "This", "The", etc.
            bool startsWithSubject = Regex.IsMatch(text, @"^(I|My|This|The|It|There is|\b[A-Z][a-z]+)\b");

            // No question marks
            bool noQuestionMarks = !text.Contains("?");

            // Ends with a strong punctuation mark
            bool endsWithPunctuation = text.EndsWith(".") || text.EndsWith("!");

            // Keep it simple — under 2 commas
            int commaCount = text.Count(c => c == ',');
            bool lowClauseComplexity = commaCount < 2;

            return startsWithSubject && noQuestionMarks && endsWithPunctuation && lowClauseComplexity;
        }
    }
}
