using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.Linq;
using ContingentParser;
using System;
using DocumentFormat.OpenXml.Packaging;

namespace ConsoleApp.InputFilesParser
{
    class ResidualKnowledgeDataParser
    {
        // private int studentNameIndex = 4; // 0-based 
        // private int startQuestionsIndex = 8; // 0-based
        // private int shift = 3; // число колонок с информацией об одном ответе

        private MsFormsParser msFormsParser;
        private FormWithQuestionsAndPossibleAnswersParser testFormParser;
        private MidCertificationResultsParser midCertificationResultsParser;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msForms">MS Forms Document с результатами тестирования ОЗ дисциплины</param>
        /// <param name="discipline">Дисциплина ОЗ по которой тестируются</param>
        /// <param name="students">Отфильтрованные студенты -- направление конкретное</param>
        /// <param name="authorOfRightAnswersFullName">Имя человека, который ввел правильные ответы -- составитель теста</param>
        public ResidualKnowledgeDataParser(CheckingDiscipline discipline, List<Student> students)
        {           
            msFormsParser = new MsFormsParser(discipline, students);
            testFormParser = new FormWithQuestionsAndPossibleAnswersParser(discipline.TxtTestFormPath);
            midCertificationResultsParser = new MidCertificationResultsParser(discipline);
        }

        public ResidualKnowledgeDataParserResult Parse()
        {
            var formulationsParsingSuccess = msFormsParser.TryParseFormulations(out var formulations);
            var testFormParsingSuccess = testFormParser.TryParseQuestionsAndPossibleAnswers(out var testForm); 

            List<(int QuestionNumber, List<Answer> RightAnswers)> rightAnswers;
            var possibleAnswers = testForm.Select(pa => (pa.QuestionNumber, pa.PossibleAnswers)).ToList();
            var rightAnswersParsingSuccess = testFormParsingSuccess
                ? msFormsParser.TryParseRightAnswersOfTestAuthor(possibleAnswers, out rightAnswers)
                : msFormsParser.TryParseRightAnswersOfTestAuthor(out rightAnswers);

            var questions = new List<Question>();
            if (formulationsParsingSuccess)
            {
                formulations.ForEach(f => 
                {
                    (_, _, List<Answer> possibleAnswers, int? maxScore) = testForm.FirstOrDefault(pa => pa.QuestionNumber == f.QuestionNumber);
                    var (_, expectedAnswers) = rightAnswers.FirstOrDefault(ra => ra.QuestionNumber == f.QuestionNumber);
                    questions.Add(new Question(f.QuestionNumber, f.Formulation, expectedAnswers, possibleAnswers, maxScore));
                });
            }   
            else if (testFormParsingSuccess)
            {
                testForm.ForEach(pa => 
                {
                    var (_, expectedAnswers) = rightAnswers.FirstOrDefault(ra => ra.QuestionNumber == pa.QuestionNumber);                      
                    questions.Add(new Question(pa.QuestionNumber, pa.Formulation, expectedAnswers, pa.PossibleAnswers, pa.MaxScore));
                });
            }
            else if (rightAnswersParsingSuccess)
            {
                rightAnswers.ForEach(ra => questions.Add(new Question(ra.QuestionNumber, ra.RightAnswers)));
            }
            
            var studentAnswersSuccess = msFormsParser.TryParseStudentAnswers(questions, out var answers);
            var studentAnswers = studentAnswersSuccess 
                ? new List<StudentAnswer>(answers) 
                : new List<StudentAnswer>(); 

            var midCertificationParsingSuccess = midCertificationResultsParser.TryParseMidCertificationResults(out var midCerificationResults);
            
            return new ResidualKnowledgeDataParserResult(questions, studentAnswers, midCerificationResults,
                formulationsParsingSuccess, testFormParsingSuccess, rightAnswersParsingSuccess,
                studentAnswersSuccess, midCertificationParsingSuccess);
        }
    }
}
