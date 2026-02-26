using System;
using System.Collections.Generic;
using LanguageTeacher.ConsoleApp.Services.StudyService;
using LanguageTeacher.ConsoleApp.Services.StudyService.Entities;
using LanguageTeacher.DataAccess.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LanguageTeacher.ConsoleApp.ConsoleFramework
{
    static class ConsoleASCII
    {
        const int column_width = 16, n_width = column_width*4+3*2+2;

        public static void WriteVocabularTable(ICollection<VerbalEntry> pairs)
        {
            WriteTableHeader();

            foreach (var pair in pairs)
                WriteTableBody(pair);

            WriteTableFooter();
        }

        public static void WriteQuestionTable(ICollection<Question> questions)
        {
            WriteTableHeader();

            foreach (var quest in questions)
                Console.WriteLine($"│ {quest.Foreign,-20} │ {quest.UserAnswer}");

            WriteTableFooter();
        }

        public static void WriteTableHeader()
        {
            Console.Write('\u2552');
            Console.Write(new string('\u2550', n_width));
            Console.WriteLine('\u2555');
        }

        public static void WriteTableBody(VerbalEntry pair)
        {
            //string key_column = $"[{pair.Id}] {pair.Foreign.Capitalize()} ";
            string key_column = $"{pair.Foreign.Capitalize()} ";
            Console.Write($"\u2502 {key_column,-column_width} ");

            string transcript_column = pair.Transcription ?? "";
            Console.Write($"\u2502 {transcript_column,-column_width} ");

            string translations_column = string.Join(", ", pair.Translations).Capitalize();
            Console.Write($"\u2502 {translations_column, -column_width*2} \u2502\n");

            if (pair.Examples.Count > 0)
            {
                foreach (var example in pair.Examples)
                {
                    Console.Write("\u2502");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"{" – " + example.Capitalize(),-n_width}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("\u2502\n");
                }
            }
        }

        public static void WriteTableFooter()
        {
            Console.Write('\u2558');
            Console.Write(new string('\u2550', n_width));
            Console.WriteLine('\u255B');
        }

        public static void WriteError(string msg)
        {
            Console.ForegroundColor= ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor= ConsoleColor.Gray;
        }
    }
}
