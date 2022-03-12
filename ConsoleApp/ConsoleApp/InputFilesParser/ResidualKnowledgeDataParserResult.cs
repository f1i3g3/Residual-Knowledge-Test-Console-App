using System.Collections.Generic;

namespace ConsoleApp.InputFilesParser
{
    /// <summary>
    /// Парсит результаты MsForms (xlsx), тестовую форму (pdf), результаты промежуточной аттестации (docx / xlsx?). 
    /// В результате получаем:
    /// 1) List<Question> Questions для checkingDiscipline, кторые нужны исключительно для генерации файла ФОС
    /// 2) List<StudentAnswer>, которые нужны для вставки в самое начало гугл-таблицы
    /// </summary>
    /// 
    class ResidualKnowledgeDataParserResult
    {
        public ResidualKnowledgeDataParserResult(List<Question> questions, List<StudentAnswer> studentAnswers, 
            List<MidCerificationAssesmentResult> midCerificationResults, bool formulationsParsingSuccess, 
            bool testFormParsingSuccess, bool rightAnswersParsingSuccess, bool studentAnswersParsingSuccess, 
            bool midCertificationParsingSuccess)
        {
            Questions = questions;
            StudentAnswers = studentAnswers;
            MidCerificationResults = midCerificationResults;
            FormulationsParsingSuccess = formulationsParsingSuccess;
            TestFormParsingSuccess = testFormParsingSuccess;
            RightAnswersParsingSuccess = rightAnswersParsingSuccess;
            StudentAnswersParsingSuccess = studentAnswersParsingSuccess;
            MidCertificationParsingSuccess = midCertificationParsingSuccess;
        }

        public List<Question> Questions { get; private set; }

        public List<StudentAnswer> StudentAnswers { get; private set; }

        public List<MidCerificationAssesmentResult> MidCerificationResults { get; private set; }

        public bool FormulationsParsingSuccess { get; private set; } // из xlsx MS Forms

        public bool TestFormParsingSuccess { get; private set; } // pdf

        public bool RightAnswersParsingSuccess { get; private set; } // xlsx MS Forms

        public bool StudentAnswersParsingSuccess { get; private set; } // xlsx MS Forms

        public bool MidCertificationParsingSuccess { get; private set; } // docx
    }
}
