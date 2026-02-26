using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.Interfaces;
using LanguageTeacher.ConsoleApp.Services.LearningService.Strategies;
using LanguageTeacher.ConsoleApp.Services.StudyService.Entities;
using LanguageTeacher.DataAccess.Data.Entities;

namespace LanguageTeacher.ConsoleApp.Services.StudyService
{
    public class LearningService : ILearningService
    {
        private IVocabularService _vocabularService;
        private LearningSession? _session;


        public LearningService(IVocabularService vocabularService)
        {
            _vocabularService = vocabularService;
        }


        public bool HasOpenSession()
        {
            return _session != null;
        }

        public ICollection<Question> GetQuestions()
        {
            if (_session == null)
                throw new ArgumentException("Learning session is null.");

            return _session.Questions;
        }

        public void OpenSession(IQuestionStrategy strategy)
        {
            var entries = _vocabularService.GetAll();
            var questions = strategy.GetArray(entries);

            _session = new LearningSession(questions);
        }

        public SessionResult CloseSession()
        {
            if (_session == null)
                throw new ArgumentException("An session to close is not found.");

            SessionResult result = _session.CloseAndGetResult(_vocabularService);
            _session = null;

            return result;
        }

        public void SendAnswer(string answer)
        {
            if (_session == null)
                throw new ArgumentException("Learning session is null.");

            _session.NextQuestion(answer);
        }
    }
}
