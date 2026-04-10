using Itero.API.Data.Entities;
using Itero.API.Dtos;

namespace Itero.API.Data
{
    public class VocabularyEntryMapper
    {
        public VocabularyEntry GetEntry(VocabularyEntryDto entryDto, User user)
        {
            string foreign = PrepareForeign(entryDto.Foreign);
            string trascritpion = PrepareTranscription(entryDto.Transcription);
            var examples = PrepareExamples(entryDto.Examples);
            var translations = PrepareTranslations(entryDto.Translations);


            var resultEntry = new VocabularyEntry();
            resultEntry.User = user;

            resultEntry.Foreign = foreign;
            resultEntry.Transcription = trascritpion;
            resultEntry.Examples = examples;
            resultEntry.Translations = translations;

            return resultEntry;
        }

        public VocabularyEntryDto GetDto(VocabularyEntry entry)
        {
            string foreign = PrepareForeign(entry.Foreign);
            string trascritpion = PrepareTranscription(entry.Transcription);
            var examples = PrepareExamples(entry.Examples.ToArray());
            var translations = PrepareTranslations(entry.Translations.ToArray());


            var entryDto = new VocabularyEntryDto();

            entryDto.Foreign = foreign;
            entryDto.Transcription = trascritpion;
            entryDto.Examples = examples.ToArray();
            entryDto.Translations = translations.ToArray();

            return entryDto;
        }

        public VocabularyEntry GetPatched(VocabularyEntry baseEntry, VocabularyPatchDto patchDto)
        {
            var entry = baseEntry;

            // Foreign patch
            if (patchDto.Foreign != null)
                entry.Foreign = PrepareForeign(patchDto.Foreign);

            // Transcription patch
            if (patchDto.Transcription != null)
                entry.Transcription = PrepareTranscription(patchDto.Transcription);

            // Examples add
            if (patchDto.ExamplesAdd != null)
                entry.Examples.AddRange(PrepareExamples(patchDto.ExamplesAdd));

            // Examples remove
            if (patchDto.ExamplesRemove != null)
            {
                var examplesToRemove = new HashSet<string>(PrepareExamples(patchDto.ExamplesRemove));
                entry.Examples.RemoveAll(examplesToRemove.Contains);
            }

            // Translations add
            if (patchDto.TranslationsAdd != null)
                entry.Translations.AddRange(PrepareTranslations(patchDto.TranslationsAdd));

            // Translations remove
            if (patchDto.TranslationsRemove != null)
            {
                var translationsToRemove = new HashSet<string>(PrepareTranslations(patchDto.TranslationsRemove));
                entry.Translations.RemoveAll(translationsToRemove.Contains);
            }


            return entry;
        }


        public string PrepareForeign(string foreign)
        {
            return foreign.RemoveMultispaces()
                .ToLowerInvariant();
        }

        public string PrepareTranscription(string trascription)
        {
            return trascription.RemoveMultispaces()
                .ToLowerInvariant()
                .WrapWithBracketsIfNeeded();
        }

        public List<string> PrepareExamples(string[] examples)
        {
            var result = new List<string>();

            foreach (var e in examples)
            {
                string item = e.RemoveMultispaces()
                    .ToLowerInvariant()
                    .AddPointIfNeeded();

                if (result.Contains(item))
                    continue;

                result.Add(item);
            }

            return result;
        }

        public List<string> PrepareTranslations(string[] translations)
        {
            var result = new List<string>();

            foreach (var e in translations)
            {
                string item = e.RemoveMultispaces()
                    .ToLowerInvariant();

                if (result.Contains(item))
                    continue;

                result.Add(item);
            }

            return result;
        }
    }
}
