namespace ConsoleApp.GoogleSpreadsheetParser
{
    class DisciplineAssesmentResult
    {
        public DisciplineAssesmentResult(string student, string discipline,
            string competenceAssessmentMark, string midtermCertificationMark)
        {
            Student = student;
            Discipline = discipline;
            CompetenceAssessmentMark = competenceAssessmentMark;
            MidtermCertificationMark = midtermCertificationMark;
        }

        public string Student { get; private set; }

        public string Discipline { get; private set; }

        public string MidtermCertificationMark { get; private set; }

        public string CompetenceAssessmentMark { get; private set; }
    }
}
