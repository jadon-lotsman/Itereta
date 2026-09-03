namespace Mnemo.Data.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public DateTime RegisteredAt { get; set; }


        public List<Vocabulary> Vocabularies { get; set; }
        public List<RepetitionTask> RepetitionTasks { get; set; }


        public User()
        {
            RegisteredAt = DateTime.UtcNow;

            Vocabularies = new List<Vocabulary>();
            RepetitionTasks = new List<RepetitionTask>();
        }
    }
}
