using Microsoft.Extensions.Options;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Services.RepetitionService.Providers.DistractorProviders;

namespace Mnemo.Services.RepetitionService.Factories
{
    public class RepetitionTaskFactory
    {
        private readonly IOptions<RepetitionOptions> _options;
        private readonly IDistractorProvider _provider;
        private readonly SyllableDistractorProvider _syllable;
        private readonly VocabularyEntryQueries _vocabularyQueries;

        public RepetitionTaskFactory(IOptions<RepetitionOptions> options, IDistractorProvider provider, SyllableDistractorProvider syllable, VocabularyEntryQueries vocabularyQueries)
        {
            _options = options;
            _provider = provider;
            _syllable = syllable;
            _vocabularyQueries = vocabularyQueries;
        }


        public async Task<RepetitionTask> CreateByTypeAsync(bool isForward, Type taskType, VocabularyEntry baseEntry, params int[] excludeIds)
        {
            bool showPartOfSpeechLabel = await _vocabularyQueries.HasAlternativePartOfSpeechAsync(baseEntry.Vocabulary.OwnerId, baseEntry.Foreign, baseEntry.PartOfSpeech) && isForward;


            if (taskType == typeof(OptionRepetitionTask))
            {
                int take = _options.Value.OptionsDistractorCount;

                var distractors = await _provider.GetDistractorsAsync(isForward, baseEntry, take, excludeIds);
                if (distractors.Count < take)
                    return CreateTextTask(isForward, showPartOfSpeechLabel, baseEntry);

                return CreateOptionsTask(isForward, showPartOfSpeechLabel, baseEntry, distractors);
            }
            else if (taskType == typeof(SentenceReorderRepetitionTask))
            {
                var filtered = baseEntry.Examples.Where(e => e.Split().Length >= 3).ToList();

                if (!filtered.Any())
                    return CreateTextTask(isForward, showPartOfSpeechLabel, baseEntry);

                return CreateSentenceReorderTask(baseEntry, filtered);
            }
            else if (taskType == typeof(SyllableReorderRepetitionTask) && baseEntry.Foreign.Length > 8)
            {
                int take = 2;

                var distractors = await _syllable.GetDistractorsAsync(false, baseEntry, take, excludeIds);
                if (distractors.Count < take)
                    return CreateTextTask(isForward, showPartOfSpeechLabel, baseEntry);

                return CreateSyllableReorderTask(showPartOfSpeechLabel, baseEntry, distractors);
            }
            else if (taskType == typeof(YesOrNoRepetitionTask))
            {
                var distractors = await _provider.GetDistractorsAsync(true, baseEntry, 1, excludeIds);
                if (distractors.Count < 1)
                    return CreateTextTask(isForward, showPartOfSpeechLabel, baseEntry);

                return CreateYesOrNoTask(baseEntry, distractors[0]);
            }

            return CreateTextTask(isForward, showPartOfSpeechLabel, baseEntry);
        }

        public TextRepetitionTask CreateTextTask(bool isForward, bool showPartOfSpeechLabel, VocabularyEntry baseEntry)
        {
            string prompt = isForward ? baseEntry.Foreign : baseEntry.Translations[0];
            var partOfSpeech = showPartOfSpeechLabel ? baseEntry.PartOfSpeech : null;

            var correct = isForward ? baseEntry.Translations : [baseEntry.Foreign];

            return new TextRepetitionTask(prompt, partOfSpeech, baseEntry.Vocabulary.OwnerId, baseEntry.Id, correct);
        }

        public OptionRepetitionTask CreateOptionsTask(bool isForward, bool showPartOfSpeechLabel, VocabularyEntry baseEntry, List<string> distractors)
        {
            string prompt = isForward ? baseEntry.Foreign : baseEntry.Translations[0];
            var partOfSpeech = showPartOfSpeechLabel ? baseEntry.PartOfSpeech : null;

            var correct = isForward ? baseEntry.Translations[0] : baseEntry.Foreign;

            return new OptionRepetitionTask(prompt, partOfSpeech, baseEntry.Vocabulary.OwnerId, baseEntry.Id, distractors, correct);
        }

        public SentenceReorderRepetitionTask CreateSentenceReorderTask(VocabularyEntry baseEntry, List<string> sentences)
        {
            int index = Random.Shared.Next(sentences.Count);
            var sentence = sentences[index];

            return new SentenceReorderRepetitionTask(baseEntry.Vocabulary.OwnerId, baseEntry.Id, sentence);
        }

        public SyllableReorderRepetitionTask CreateSyllableReorderTask(bool showPartOfSpeechLabel, VocabularyEntry baseEntry, List<string> distractors)
        {
            var foreign = baseEntry.Foreign;
            var partOfSpeech = showPartOfSpeechLabel ? baseEntry.PartOfSpeech : null;

            return new SyllableReorderRepetitionTask(partOfSpeech, baseEntry.Vocabulary.OwnerId, baseEntry.Id, foreign, distractors);
        }

        public YesOrNoRepetitionTask CreateYesOrNoTask(VocabularyEntry baseEntry, string distractor)
        {
            string prompt = baseEntry.Foreign;

            string option;

            bool isCorrect = Random.Shared.Next(2) == 0;
            if (isCorrect)
            {
                int index = Random.Shared.Next(baseEntry.Translations.Count);
                option = baseEntry.Translations[index];
            }
            else
            {
                option = distractor;
            }

            return new YesOrNoRepetitionTask(prompt, baseEntry.Vocabulary.OwnerId, baseEntry.Id, option, isCorrect);
        }
    }
}
