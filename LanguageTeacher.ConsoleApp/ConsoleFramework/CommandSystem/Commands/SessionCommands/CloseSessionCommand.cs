using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.Interfaces;
using LanguageTeacher.ConsoleApp.Services.StudyService;

namespace LanguageTeacher.ConsoleApp.ConsoleFramework.CommandSystem.Commands.SessionCommands
{
    public class CloseSessionCommand : CommandBase
    {
        protected override int ExpectedArgsCount => 0;
        protected override bool HasLimitlessArgs => false;

        private readonly ILearningService _service;


        public CloseSessionCommand(ILearningService service)
        {
            _service = service;
        }


        protected override void ExecuteInternal(string[] args)
        {
            var result = _service.CloseSession();

            Console.WriteLine(result);
            Console.ReadLine();
        }
    }
}
