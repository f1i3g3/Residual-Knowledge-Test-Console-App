using System.Collections.Generic;
using ContingentParser;

namespace ConsoleApp
{
    // мб модель должна быть другой
    class StudentAnswer
    {
        public StudentAnswer(Student student, CheckingDiscipline discipline, Dictionary<Question, int> scores)
        {
            Student = student;
            Discipline = discipline;
            Scores = scores;
        }

        public Student Student { get; private set; }

        public CheckingDiscipline Discipline { get; private set; }

        public Dictionary<Question, int> Scores { get; private set; } // int по логике
    }
}
