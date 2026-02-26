using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.ConsoleFramework.CommandSystem.Commands;
using LanguageTeacher.ConsoleApp.ConsoleFramework.CommandSystem.Commands.SessionCommands;
using LanguageTeacher.ConsoleApp.ConsoleFramework.CommandSystem.Interfaces;
using LanguageTeacher.ConsoleApp.Interfaces;

namespace LanguageTeacher.ConsoleApp.ConsoleFramework.CommandSystem
{
    public class CommandFactory
    {
        public ICommand Create(string command, IVocabularService _vocabService, ILearningService _studyService)
        {
            return command.ToLower() switch
            {
                "add" => new AddEntryCommand(_vocabService),
                "example" =>  new AddEntryExampleCommand(_vocabService),
                "pronun" => new AddEntryTranscriptionCommand(_vocabService),
                "remove" => new RemoveEntryCommand(_vocabService),
                "session" => new OpenSessionCommand(_studyService),
                "answer" => new SendAnswerCommand(_studyService),
                "result" => new CloseSessionCommand(_studyService),
                _ => throw new ArgumentException($"Unknown command with name '{command}'")
            };
        }
    }
}
