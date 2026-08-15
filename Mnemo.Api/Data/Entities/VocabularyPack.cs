namespace Mnemo.Data.Entities
{
    public class VocabularyPack
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        public int AuthorId { get; set; }
        public User Author { get; set; }
        public List<VocabularyPackEntry> PackEntries { get; set; }



        public VocabularyPack()
        {
            PackEntries = new List<VocabularyPackEntry>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
        }
    }
}
