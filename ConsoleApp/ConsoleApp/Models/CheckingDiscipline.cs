using CurriculumParser;
using System.Collections.Generic;

namespace ConsoleApp
{
    public class CheckingDiscipline
    {
        public CheckingDiscipline(Discipline discipline, List<Competence> checkingCompetences, User user, 
            List<MarkCriterion> scale = null, List<Question> questions = null, int questionsCount = 0, 
            string msFormsPath = "", MsFormsParserConfiguration config = null, string txtTestFormPath = "", 
            string midCerificationResultsPath = "")
        {
            Discipline = discipline;
            CheckingCompetences = checkingCompetences;
            Scale = scale ?? new List<MarkCriterion>();
            Questions = questions ?? new List<Question>();
            QuestionsCount = Questions.Count == 0 ? questionsCount : Questions.Count;
            MsFormsPath = msFormsPath;
            MsFormsConfig = config ?? new MsFormsParserConfiguration(4, 8, 3, user);
            TxtTestFormPath = txtTestFormPath;
            MidCerificationResultsPath = midCerificationResultsPath;
        }

        public Discipline Discipline { get; private set; }

        public List<Competence> CheckingCompetences { get; private set; }

        public List<MarkCriterion> Scale { get; private set; }

        public List<Question> Questions { get; private set; }

        public int QuestionsCount { get; private set; }

        public string MsFormsPath { get; private set; }

        public MsFormsParserConfiguration MsFormsConfig { get; private set; }

        public string TxtTestFormPath { get; private set; }

        public string MidCerificationResultsPath { get; private set; }
    }
}
