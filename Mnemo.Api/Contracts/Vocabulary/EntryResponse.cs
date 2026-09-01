namespace Mnemo.Contracts.Vocabulary
{
    public class EntryResponse
    {
        public int Id { get; set; }
        public string? PartOfSpeech { get; set; }
        public string? CERF { get; set; }
        public string? Foreign { get; set; }
        public string? Transcription { get; set; }
        public string? AudioUrl { get; set; }
        public string[]? Examples { get; set; }
        public string[]? Translations { get; set; }
        public string[]? Synonyms { get; set; }
        public string[]? Antonyms { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
