using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itereta.Data.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }
        public DateTime RegisteredAt { get; set; }


        public Iteration? Iteration { get; set; }
        public List<VocabularyEntry> Entries { get; set; }
        public List<RepetitionState> RepetitionStates { get; set; }


        public User() {}

        public User(string username)
        {
            Username = username;
            RegisteredAt = DateTime.UtcNow;

            Entries = new List<VocabularyEntry>();
            RepetitionStates = new List<RepetitionState>();
        }
    }
}
