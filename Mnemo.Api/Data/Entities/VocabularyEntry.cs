using Mnemo.Contracts.Vocabulary;
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


        public int UserId { get; set; }
        public User User { get; set; }
        public int? SourcePackId { get; set; }
        public VocabularyPack? SourcePack { get; set; }
        public RepetitionState? RepetitionState { get; set; }


        public VocabularyEntry()
        {
            EnrichmentStatus = EnrichmentStatus.Pending;
            LastEnrichmentAt = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
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
