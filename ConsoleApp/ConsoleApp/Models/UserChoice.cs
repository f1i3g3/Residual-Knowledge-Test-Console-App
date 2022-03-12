using ContingentParser;
using CurriculumParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp
{
    class UserChoice
    {
        public UserChoice(User user, ICurriculumWithElectiveBlocks curriculum, 
            Contingent contingent, List<CheckingDiscipline> checkingDisciplines)
        {
            User = user;
            Curriculum = curriculum;
            Students = contingent
                .Where(s => s.CurriculumCode == curriculum.CurriculumCode.Replace("/", "\\"))
                .OrderBy(s => s.FullName)
                .ToList();

            CheckingDisciplines = checkingDisciplines.OrderBy(d => d.Discipline.RussianName).ToList();

            var yearOfAdmission = int.Parse(curriculum.CurriculumCode.Substring(0, 2));
            Course = DateTime.Today.Month >= 7
                    ? DateTime.Today.Year % 100 - yearOfAdmission == 0
                        ? 1
                        : DateTime.Today.Year % 100 - yearOfAdmission
                    : DateTime.Today.Year % 100 - yearOfAdmission + 1;

            var curriculumCode = curriculum.CurriculumCode.Substring(3, 4);
            StrangeCode = curriculum.Programme.LevelOfEducation == "магистратура"
                ? $"ВМ.{curriculumCode}"
                : $"СВ.{curriculumCode}";
        }

        public User User { get; private set; }

        public ICurriculumWithElectiveBlocks Curriculum { get; private set; }

        public int Course { get; private set; } // должно быть в Curriculum

        public string StrangeCode { get; private set; } // должно быть в Curriculum ВМ.5006

        public List<Student> Students { get; private set; }

        public List<CheckingDiscipline> CheckingDisciplines { get; private set; }
    }
}
