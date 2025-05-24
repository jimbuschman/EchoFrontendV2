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
        private static readonly HashSet<string> NoisePhrases = new(StringComparer.OrdinalIgnoreCase)
        {
        // Greetings
        "hi", "hello", "hey", "hi there", "hey there", "what's up", "howdy",
        "good morning", "good afternoon", "good night", "bye", "see ya",

        // Short affirmatives/negatives
        "ok", "okay", "yeah", "nah", "maybe", "got it", "roger", "sure",
        "yes", "no", "yep", "nope", "alright", "uh-huh", "mm-hmm",

        // Generic reactions
        "wow", "oh", "ah", "oops", "whoops", "hm", "hmm", "heh", "cool", "nice", "great", "awesome",

        // Internet/text slang
        "lol", "haha", "lmao", "lmfao", "rofl", "smh", "brb", "btw", "idk", "imo", "tbh", "omg"
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

            string normalized = text.Trim().ToLowerInvariant();

            // Match exact known noise phrases
            if (NoisePhrases.Contains(normalized))
                return true;

            // Check for short filler words alone
            if (FillerWords.Contains(normalized))
                return true;

            // Very short message like "ok." or "yes!" (under 6 chars, alpha + punctuation)
            if (normalized.Length <= 6 && Regex.IsMatch(normalized, @"^[a-z]+[.!]*$"))
                return true;

            // Laughter or slang patterns
            if (Regex.IsMatch(normalized, @"^(ha|lol|lmao|rofl)+[!]*$", RegexOptions.IgnoreCase))
                return true;

            return false;
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
