using Mnemo.Shared.Enums;

namespace Mnemo.Data.Entities
{
    public abstract class VocabularyDefinition
    {
        public PartOfSpeech? PartOfSpeech { get; set; }
        public CEFRLevel? CEFR { get; set; }
        public string Foreign { get; set; }
        public string? Transcription { get; set; }
        public string? AudioUrl { get; set; }
        public List<string> Examples { get; set; }
        public List<string> Translations { get; set; }
        public List<string> Synonyms { get; set; }
        public List<string> Antonyms { get; set; }



        public VocabularyDefinition()
        {
            Examples = new List<string>();
            Translations = new List<string>();
            Synonyms = new List<string>();
            Antonyms = new List<string>();
        }
    }
}
