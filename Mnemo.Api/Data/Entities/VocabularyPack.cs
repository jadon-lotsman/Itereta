using Mnemo.Shared.Enums;

namespace Mnemo.Data.Entities
{
    public class VocabularyPack
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Visibility Visibility { get; set; }
        public int ImportCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        public int AuthorId { get; set; }
        public User Author { get; set; }
        public List<VocabularyPackEntry> PackEntries { get; set; }


        public VocabularyPack()
        {
            Visibility = Visibility.Private;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;

            Guid = Guid.NewGuid();
            PackEntries = new List<VocabularyPackEntry>();
        }
    }
}
