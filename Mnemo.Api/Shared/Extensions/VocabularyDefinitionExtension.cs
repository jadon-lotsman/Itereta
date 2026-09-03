using Mnemo.Contracts.Entry.Requests;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;

namespace Mnemo.Shared.Extensions
{
    public static class VocabularyDefinitionExtension
    {
        public static bool TryPatch(this VocabularyDefinition entry, PatchEntryRequest patch)
        {
            PartOfSpeech? _partOfSpeech = null;
            if (patch.PartOfSpeech != null)
            {
                if (!Enum.TryParse<PartOfSpeech>(patch.PartOfSpeech, true, out var parsedPartOfSpeech))
                    return false;

                _partOfSpeech = parsedPartOfSpeech;
            }

            CEFRLevel? _cefr = null;
            if (patch.CEFR != null)
            {
                if (!Enum.TryParse<CEFRLevel>(patch.CEFR, true, out var parsedCEFRLevel))
                    return false;

                _cefr = parsedCEFRLevel;
            }

            string? _foreign = null;
            if (patch.Foreign != null)
            {
                string normalized = TextNormalizer.NormalizeForeign(patch.Foreign);
                if (string.IsNullOrWhiteSpace(normalized))
                    return false;

                _foreign = normalized;
            }

            string? _transcription = null;
            if (patch.Transcription != null)
            {
                string normalized = TextNormalizer.NormalizeTranscription(patch.Transcription);
                if (string.IsNullOrWhiteSpace(normalized))
                    return false;

                _transcription = normalized;
            }


            List<string>? _examplesToAdd = null;
            if (patch.ExamplesAdd != null)
                _examplesToAdd = TextNormalizer.NormalizeEnumerable(patch.ExamplesAdd, TextNormalizer.NormalizeExample);

            List<string>? _examplesToRemove = null;
            if (patch.ExamplesRemove != null)
                _examplesToRemove = TextNormalizer.NormalizeEnumerable(patch.ExamplesRemove, TextNormalizer.NormalizeExample);

            List<string>? _translationsToAdd = null;
            if (patch.TranslationsAdd != null)
                _translationsToAdd = TextNormalizer.NormalizeEnumerable(patch.TranslationsAdd, TextNormalizer.NormalizeTranslation);

            List<string>? _translationsToRemove = null;
            if (patch.TranslationsRemove != null)
                _translationsToRemove = TextNormalizer.NormalizeEnumerable(patch.TranslationsRemove, TextNormalizer.NormalizeTranslation);

            List<string>? _synonymsToAdd = null;
            if (patch.SynonymsAdd != null)
                _synonymsToAdd = TextNormalizer.NormalizeEnumerable(patch.SynonymsAdd, TextNormalizer.NormalizeForeign);

            List<string>? _synonymsToRemove = null;
            if (patch.SynonymsRemove != null)
                _synonymsToRemove = TextNormalizer.NormalizeEnumerable(patch.SynonymsRemove, TextNormalizer.NormalizeForeign);

            List<string>? _antonymsToAdd = null;
            if (patch.AntonymsAdd != null)
                _antonymsToAdd = TextNormalizer.NormalizeEnumerable(patch.AntonymsAdd, TextNormalizer.NormalizeForeign);

            List<string>? _antonymsToRemove = null;
            if (patch.AntonymsRemove != null)
                _antonymsToRemove = TextNormalizer.NormalizeEnumerable(patch.AntonymsRemove, TextNormalizer.NormalizeForeign);


            if (_partOfSpeech != null)
                entry.PartOfSpeech = _partOfSpeech.Value;
            if (_cefr != null)
                entry.CEFR = _cefr.Value;
            if (_foreign != null)
                entry.Foreign = _foreign;
            if (_transcription != null)
                entry.Transcription = _transcription;

            if (_examplesToAdd != null)
                entry.Examples.AddRange(_examplesToAdd);
            if (_examplesToRemove != null)
                entry.Examples.RemoveAll(_examplesToRemove.Contains);

            if (_translationsToAdd != null)
                entry.Translations.AddRange(_translationsToAdd);
            if (_translationsToRemove != null)
                entry.Translations.RemoveAll(_translationsToRemove.Contains);

            if (_synonymsToAdd != null)
                entry.Synonyms.AddRange(_synonymsToAdd);
            if (_synonymsToRemove != null)
                entry.Synonyms.RemoveAll(_synonymsToRemove.Contains);

            if (_antonymsToAdd != null)
                entry.Antonyms.AddRange(_antonymsToAdd);
            if (_antonymsToRemove != null)
                entry.Antonyms.RemoveAll(_antonymsToRemove.Contains);

            return true;
        }
    }
}
