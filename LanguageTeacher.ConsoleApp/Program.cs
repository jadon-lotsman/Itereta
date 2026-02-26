using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using LanguageTeacher.ConsoleApp.ConsoleFramework;
using LanguageTeacher.ConsoleApp.ConsoleFramework.CommandSystem;
using LanguageTeacher.ConsoleApp.Services.StudyService;
using LanguageTeacher.ConsoleApp.Services.VocabularService;
using LanguageTeacher.DataAccess.Data.Entities;

namespace LanguageTeacher.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var vocabService = new VocabularService();
            var studyService = new LearningService(vocabService);
            var cmdExecutor = new CommandExecutor(vocabService, studyService);

            while (true)
            {
                Console.Clear();

                if (studyService.HasOpenSession())
                {
                    ConsoleASCII.WriteQuestionTable(studyService.GetQuestions());
                }
                else
                {
                    ConsoleASCII.WriteVocabularTable(vocabService.GetAll());
                }

                Console.Write("> ");

                string? userRequest = Console.ReadLine();

                try
                {
                    cmdExecutor.ExecuteUserCommand(userRequest);
                }
                catch (Exception ex)
                {
                    ConsoleASCII.WriteError(ex.Message);
                    Console.ReadLine();
                }
            }
        }
    }
}
