using Mnemo.Shared.Enums;
using System.Text.RegularExpressions;

namespace Mnemo.Controllers.Validation
{
    public static class VocabularyDefinitionRules
    {
        private static readonly Regex _foreignRegex = new(
            @"^[\p{L}\s'\-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<char> _forbiddenChars = new() {
            '@', '#', '$', '%', '^', '&', '*', '(', ')', '=', '+', '{', '}', '<', '>', '\\', '|', '~'};

        private static readonly Regex _translationRegex = new(
            @"^[\p{L}\s\'\.\,]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static bool IsValidPartOfSpeech(string? partOfSpeech)
        {
            if (string.IsNullOrWhiteSpace(partOfSpeech))
                return false;

            return Enum.TryParse<PartOfSpeech>(partOfSpeech, true, out _);
        }

        public static bool IsValidCEFRLevel(string? cefr)
        {
            if (string.IsNullOrWhiteSpace(cefr))
                return false;

            return Enum.TryParse<CEFRLevel>(cefr, true, out _);
        }

        public static bool IsValidForeign(string? foreign)
        {
            if (string.IsNullOrWhiteSpace(foreign))
                return false;

            var trimmed = foreign.Trim();
            if (!trimmed.Any(char.IsLetter))
                return false;

            if (trimmed.Length > 45)
                return false;

            return _foreignRegex.IsMatch(trimmed);
        }

        public static bool IsValidTranscription(string? transcription)
        {
            if (string.IsNullOrWhiteSpace(transcription))
                return false;

            var trimmed = transcription.Trim();
            if ((trimmed.StartsWith('[') && trimmed.EndsWith(']')) ||
                (trimmed.StartsWith('/') && trimmed.EndsWith('/')))
            {
                trimmed = trimmed[1..^1];
            }

            if (trimmed.Any(_forbiddenChars.Contains))
                return false;

            return trimmed.Any(char.IsLetter);
        }

        public static bool IsValidExample(string? example)
        {
            if (string.IsNullOrWhiteSpace(example))
                return false;

            var trimmed = example.Trim();

            if (trimmed.Length < 8)
                return false;

            if (!trimmed.Any(char.IsLetter))
                return false;

            if (trimmed.Any(_forbiddenChars.Contains))
                return false;

            var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length >= 2;
        }

        public static bool IsValidTranslation(string? translation)
        {
            if (string.IsNullOrWhiteSpace(translation))
                return false;

            var trimmed = translation.Trim();
            return _translationRegex.IsMatch(trimmed) && trimmed.Any(char.IsLetter) && trimmed.Length >= 2;
        }
    }
}
