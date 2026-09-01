using Mnemo.Contracts.Vocabulary.Requests;

namespace Mnemo.Contracts.Pack.Requests
{
    public class CreatePackRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Visibility { get; set; }
        public CreateEntryRequest[]? PackEntries { get; set; }
    }
}
