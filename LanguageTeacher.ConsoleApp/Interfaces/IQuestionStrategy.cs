using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageTeacher.ConsoleApp.Services.StudyService.Entities;
using LanguageTeacher.DataAccess.Data.Entities;

namespace LanguageTeacher.ConsoleApp.Interfaces
{
    public interface IQuestionStrategy
    {
        Question[] GetArray(ICollection<VerbalEntry> entries, int length=5);
    }
}
