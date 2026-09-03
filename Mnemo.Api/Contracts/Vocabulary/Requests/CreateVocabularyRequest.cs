using Mnemo.Contracts.Entry.Requests;

namespace Mnemo.Contracts.Vocabulary.Requests
{
    public class CreateVocabularyRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Visibility { get; set; }
        public CreateEntryRequest[]? Entries { get; set; }
    }
}
