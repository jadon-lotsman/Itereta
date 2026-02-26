using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.Interfaces;
using LanguageTeacher.ConsoleApp.Services.StudyService.Entities;
using LanguageTeacher.DataAccess.Data.Entities;

namespace LanguageTeacher.ConsoleApp.Services.LearningService.Strategies
{
    public class RandomQuestionStrategy : IQuestionStrategy
    {
        private static Random _random = new Random();

        public Question[] GetArray(ICollection<VerbalEntry> entries, int length)
        {
            VerbalEntry[] entriesArray = entries.ToArray();
            _random.Shuffle(entriesArray);

            List<Question> questions = new List<Question>();

            for (int i = 0; i < length; i++)
                questions.Add(new Question(entriesArray[i].Id, entriesArray[i].Foreign));

            return questions.ToArray();
        }
    }
}
