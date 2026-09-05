using Mnemo.Shared.Enums;

namespace Mnemo.Controllers.Validation
{
    public static class VocabularyRules
    {
        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var trimmed = name.Trim();
            if (!trimmed.Any(char.IsLetter))
                return false;

            if (trimmed.Length < 3 || trimmed.Length > 45)
                return false;

            return true;
        }

        public static bool IsValidDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return false;

            var trimmed = description.Trim();
            if (!trimmed.Any(char.IsLetter))
                return false;

            if (trimmed.Length < 3 || trimmed.Length > 200)
                return false;

            return true;
        }

        public static bool IsValidVisibility(string? visibility)
        {
            if (string.IsNullOrWhiteSpace(visibility))
                return false;

            return Enum.TryParse<Visibility>(visibility, true, out _);
        }
    }
}
