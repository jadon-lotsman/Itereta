using Mnemo.Contracts.Entry;
using Mnemo.Shared.Enums;

namespace Mnemo.Data.Entities
{
    public class VocabularyEntry : VocabularyDefinition
    {
        public int Id { get; set; }
        public EnrichmentStatus EnrichmentStatus { get; set; }
        public DateTime LastEnrichmentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        public int VocabularyId { get; set; }
        public Vocabulary Vocabulary { get; set; }
        public int? MergedFromId { get; set; }
        public Vocabulary? MergedFrom { get; set; }
        public RepetitionState? RepetitionState { get; set; }


        public VocabularyEntry()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
            LastEnrichmentAt = CreatedAt;
            EnrichmentStatus = EnrichmentStatus.Pending;
        }


        public static VocabularyEntry CreateFromDefinition(VocabularyDefinition source)
        {
            return new VocabularyEntry
            {
                Foreign = source.Foreign,
                Transcription = source.Transcription,
                AudioUrl = source.AudioUrl,
                PartOfSpeech = source.PartOfSpeech,
                CEFR = source.CEFR,

                Examples = new List<string>(source.Examples),
                Translations = new List<string>(source.Translations),
                Synonyms = new List<string>(source.Synonyms),
                Antonyms = new List<string>(source.Antonyms),

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastEnrichmentAt = DateTime.UtcNow,
                EnrichmentStatus = EnrichmentStatus.Pending,
                RepetitionState = null
            };
        }

        public bool SetMeta(EnrichResponse enrich)
        {
            if (enrich == null)
                return false;

            bool isEnriched = false;

            if (Transcription == null && enrich.Transcription != null)
            {
                Transcription = enrich.Transcription;

                if (AudioUrl == null && enrich.AudioUrl != null)
                    AudioUrl = enrich.AudioUrl;

                isEnriched = true;
            }

            if (enrich.Synonyms?.Any() == true)
            {
                Synonyms = enrich.Synonyms.ToList();
                isEnriched = true;

            }

            if (enrich.Antonyms?.Any() == true)
            {
                Antonyms = enrich.Antonyms.ToList();
                isEnriched = true;
            }

            return isEnriched;
        }

        public void ResetAllMeta()
        {
            Transcription = null;
            AudioUrl = null;
            Synonyms.Clear();
            Antonyms.Clear();
            EnrichmentStatus = EnrichmentStatus.Pending;
        }

        public void ResetAudio()
        {
            AudioUrl = null;
            EnrichmentStatus = EnrichmentStatus.Pending;
        }
    }
}
