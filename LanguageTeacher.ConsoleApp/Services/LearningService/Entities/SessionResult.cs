using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.Interfaces;
using LanguageTeacher.DataAccess.Data.Entities;

namespace LanguageTeacher.ConsoleApp.Services.StudyService.Entities
{
    public class SessionResult
    {
        public int CorrectAnswersCount { get; }
        public int TotalAnswerCount { get; }


        public SessionResult(LearningSession session, IVocabularService service)
        {
            TotalAnswerCount = session.Questions.Length;

            foreach (var question in session.Questions)
            {
                int entryId = question.EntryId;
                var originaEntry = service.GetById(entryId);

                if (IsCorrectQuestion(question, originaEntry))
                {
                    CorrectAnswersCount++;
                }
            }
        }


        private bool IsCorrectQuestion(Question question, VerbalEntry entry)
        {
            string[] entryTranslations = entry.Translations.ToArray();

            if (entryTranslations.Contains(question.UserAnswer))
                return true;

            return false;
        }

        public override string ToString()
        {
            return $"{CorrectAnswersCount}/{TotalAnswerCount}";
        }
    }
}
