namespace Mnemo.Contracts.Vocabulary.Requests
{
    public class PatchEntryRequest
    {
        public string? PartOfSpeech { get; set; }
        public string? CEFR { get; set; }
        public string? Foreign { get; set; }
        public string? Transcription { get; set; }
        public string[]? ExamplesAdd { get; set; }
        public string[]? ExamplesRemove { get; set; }
        public string[]? TranslationsAdd { get; set; }
        public string[]? TranslationsRemove { get; set; }
        public string[]? SynonymsAdd { get; set; }
        public string[]? SynonymsRemove { get; set; }
        public string[]? AntonymsAdd { get; set; }
        public string[]? AntonymsRemove { get; set; }
    }
}
