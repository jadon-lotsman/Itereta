using Mnemo.Contracts.Vocabulary.Requests;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;

namespace Mnemo.Shared.Extensions
{
    public static class VocabularyExstension
    {
        public static bool TryPatch(this Vocabulary vocab, PatchVocabularyRequest patch)
        {
            string? _name = null;
            if (patch.Name != null)
            {
                var normalized = vocab.Name.RemoveMultispaces().Capitalize();
                if (string.IsNullOrWhiteSpace(normalized))
                    return false;

                _name = normalized;
            }

            string? _description = null;
            if (patch.Description != null)
            {
                var normalized = vocab.Description.RemoveMultispaces().Capitalize();
                if (string.IsNullOrWhiteSpace(_description))
                    return false;

                _description = normalized;
            }

            Visibility? _visibility = null;
            if (patch.Visibility != null)
            {
                if (!Enum.TryParse<Visibility>(patch.Visibility, true, out var parsedVisibility))
                    return false;

                _visibility = parsedVisibility;
            }


            if (_name != null)
                vocab.Name = _name;
            if (_description != null)
                vocab.Description = _description;
            if (_visibility != null)
                vocab.Visibility = _visibility.Value;

            return true;
        }
    }
}
