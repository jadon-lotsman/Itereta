namespace Mnemo.Data.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public DateTime RegisteredAt { get; set; }


        public List<RepetitionTask> RepetitionTasks { get; set; }
        public List<RepetitionState> RepetitionStates { get; set; }
        public List<VocabularyEntry> VocabularyEntries { get; set; }
        public List<VocabularyPack> VocabularyPacks { get; set; }


        public User() { }

        public User(string username)
        {
            Username = username;
            RegisteredAt = DateTime.UtcNow;

            RepetitionTasks = new List<RepetitionTask>();
            RepetitionStates = new List<RepetitionState>();
            VocabularyEntries = new List<VocabularyEntry>();
            VocabularyPacks = new List<VocabularyPack>();
        }
    }
}
