namespace Mnemo.Contracts.Vocabulary.Requests
{
    public class CreateEntryRequest
    {
        public string? PartOfSpeech { get; set; }
        public string? CERF { get; set; }
        public string? Foreign { get; set; }
        public string? Transcription { get; set; }
        public string[]? Examples { get; set; }
        public string[]? Translations { get; set; }
    }
}
