using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTeacher.ConsoleApp.Services.StudyService.Entities
{
    public class Question
    {
        public int EntryId { get; }
        public string Foreign { get; }
        public string UserAnswer { get; set; }


        public Question(int verbalId, string foreign)
        {
            EntryId = verbalId;
            Foreign = foreign;
            UserAnswer = "?";
        }
    }
}
