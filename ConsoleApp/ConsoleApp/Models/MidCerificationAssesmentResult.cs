namespace ConsoleApp
{
    class MidCerificationAssesmentResult
    {
        public MidCerificationAssesmentResult(string student, 
            string discipline, string midtermCertificationMark)
        {
            Student = student;
            Discipline = discipline;
            MidtermCertificationMark = midtermCertificationMark;
        }

        public string Student { get; private set; }

        public string Discipline { get; private set; }

        public string MidtermCertificationMark { get; private set; }
    }
}
