using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.Interfaces;

namespace LanguageTeacher.ConsoleApp.Services.StudyService.Entities
{
    public class LearningSession
    {
        public Question[] Questions { get; }

        private int _currentQuestion;


        public LearningSession(Question[] questions)
        {
            Questions = questions;
            _currentQuestion = 0;
        }


        public void NextQuestion(string answer)
        {
            Questions[_currentQuestion].UserAnswer = answer;
            _currentQuestion++;
        }

        public SessionResult CloseAndGetResult(IVocabularService service)
        {
            return new SessionResult(this, service);
        }
    }
}
