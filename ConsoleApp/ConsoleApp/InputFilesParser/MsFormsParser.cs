using System.Collections.Generic;
using System.Linq;
using System;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using ContingentParser;

namespace ConsoleApp.InputFilesParser
{
    class MsFormsParser
    { 
        public MsFormsParser(CheckingDiscipline discipline, List<Student> students)
        {
            this.discipline = discipline;
            this.students = students;
            config = discipline.MsFormsConfig;
            columnsCount = config.StartQuestionIndex + config.Shift * this.discipline.QuestionsCount;
            success = true;
            try
            {
                using var spreadsheetDocument = SpreadsheetDocument.Open(this.discipline.MsFormsPath, false); // как это работает в случае, если возникли проблемы с MsForms?
                InitSharedStrings(spreadsheetDocument);
                InitRows(spreadsheetDocument);
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private Dictionary<string, string> sharedStrings;
        private List<Row> studentAnswersRows;
        private List<Cell> rightAnswersCells;
        private List<Cell> formulationsCells;

        private readonly CheckingDiscipline discipline;
        private readonly List<Student> students;
        private readonly MsFormsParserConfiguration config;
        private readonly int columnsCount;

        private bool success;
        
        private void InitSharedStrings(SpreadsheetDocument document)
        {
            sharedStrings = new Dictionary<string, string>();
            var sharedStringTable = document.WorkbookPart.SharedStringTablePart.SharedStringTable;
            var sharedItems = sharedStringTable.Elements<SharedStringItem>().ToList();
            for (var i = 0; i < sharedItems.Count; ++i)
            {
                sharedStrings.Add(i.ToString(), sharedItems[i].InnerText);
            }
        }

        private void InitRows(SpreadsheetDocument document)
        {
            var workSheet = document.WorkbookPart.WorksheetParts.First().Worksheet;
            var sheetData = workSheet.Elements<SheetData>().First();
            var rows = sheetData.Elements<Row>().ToList();

            var user = discipline.MsFormsConfig.User;
            var authorOfRightAnswersNames = new List<string> { user.FullName, $"{user.Name} {user.Surname}", $"{user.Surname} {user.Name}" };
            var key = sharedStrings.FirstOrDefault(p => authorOfRightAnswersNames.Contains(p.Value)).Key;
            var rightAnswersRow = key != null
                ? rows.First(r => r.Elements<Cell>().ToList().Find(c => c.CellValue?.InnerText == key) != null)
                : null;
            rightAnswersCells = rightAnswersRow?.Elements<Cell>().ToList();

            formulationsCells = rows[0].Elements<Cell>().ToList();

            rows.RemoveAt(0);
            rows.Remove(rightAnswersRow);
            studentAnswersRows = rows;
        }

        /// <summary>
        /// Uses MsForms
        /// </summary>
        /// <returns></returns>
        public bool TryParseFormulations(out List<(int QuestionNumber, string Formulation)> formulations)
        {
            var questionNumber = 1;
            formulations = new List<(int QuestionNumber, string Formulation)>();
            if (!success || formulationsCells == null || sharedStrings == null)
            {
                return false;
            }
            try
            {
                for (var i = config.StartQuestionIndex; i < columnsCount; i += config.Shift)
                {
                    var formulation = sharedStrings[formulationsCells[i].CellValue.InnerText];
                    formulations.Add((questionNumber, formulation));
                    ++questionNumber;
                }
            }
            catch (IndexOutOfRangeException)
            {
                formulations.Clear(); // ?
                return false;
            }
            return true;
        }

        /// <summary>
        /// Uses MsForms
        /// </summary>
        /// <param name="possibleAnswers"></param>
        /// <returns>TryParseRightAnswersOfTestAuthorFromMsForms</returns>
        public bool TryParseRightAnswersOfTestAuthor(out List<(int QuestionNumber, List<Answer> RightAnswers)> rightAnswers)
        {   // должны выбирать из уже сформированных после парсинга ответов лмбо создавать новые, если таковых не имеется
            var questionNumber = 1;
            rightAnswers = new List<(int QuestionNumber, List<Answer> RightAnswers)>();
            if (!success || rightAnswersCells == null || sharedStrings == null)
            {
                return false;
            }
            try
            {
                for (var i = config.StartQuestionIndex; i < columnsCount; i += config.Shift)
                {
                    var questionRightAnswers = sharedStrings[rightAnswersCells[i].CellValue.InnerText].Split(';').Select(a => new Answer(a)).ToList();
                    rightAnswers.Add((questionNumber, questionRightAnswers));
                    ++questionNumber;
                }
            }
            catch (IndexOutOfRangeException)
            {
                rightAnswers.Clear(); // ?
                return false;
            }
            return true;
        }

        public bool TryParseRightAnswersOfTestAuthor(List<(int QuestionNumber, List<Answer> PossibleAnswers)> possibleAnswers,
            out List<(int QuestionNumber, List<Answer> RightAnswers)> rightAnswers)
        {   // должны выбирать из уже сформированных после парсинга ответов лмбо создавать новые, если таковых не имеется
            var questionNumber = 1;
            rightAnswers = new List<(int QuestionNumber, List<Answer> RightAnswers)>();
            if (!success || rightAnswersCells == null || sharedStrings == null)
            {
                return false;
            }

            for (var i = config.StartQuestionIndex; i < columnsCount; i += config.Shift)
            {
                var questionRightAnswers = new List<Answer>();
                List<string> answers;
                try
                {
                    answers = sharedStrings[rightAnswersCells[i].CellValue.InnerText].Split(';').ToList();
                }
                catch (IndexOutOfRangeException e)
                {
                    rightAnswers.Clear(); // ?
                    return false;
                }

                var questionRightStringAnswers = answers;
                foreach (var answer in questionRightStringAnswers)
                {
                    var a = possibleAnswers
                        .FirstOrDefault(a => a.QuestionNumber == questionNumber)
                        .PossibleAnswers
                        ?.FirstOrDefault(a => a.AnswerValue == answer);

                    if (a == null)
                    {
                        a = new Answer(answer);
                    }
                    questionRightAnswers.Add(a);
                }
                rightAnswers.Add((questionNumber, questionRightAnswers));
                ++questionNumber;
            }

            return true;
        }

        public bool TryParseStudentAnswers(List<Question> questions, out List<StudentAnswer> studentAnswers)
        {   // можно будет парсить имея список вопросов, который получается на основе msforms и txt
            // нужно будет собрать результаты всех парсингов, для каждой дисциплины собрать Questions, а затем только можно вызывать данный метод
            // List<Question> questions точно не будет null
            studentAnswers = new List<StudentAnswer>();
            if (!success || studentAnswersRows == null || sharedStrings == null)
            {
                return false;
            }
            for (var i = 0; i < studentAnswersRows.Count; ++i) //-1
            {
                try
                {
                    var cellsWithAnswers = studentAnswersRows[i].Elements<Cell>().ToList();
                    var fullName = cellsWithAnswers.Count > config.StudentNameIndex
                        ? sharedStrings[cellsWithAnswers[config.StudentNameIndex].CellValue.InnerText]
                        : throw new IndexOutOfRangeException("Incorrect StudentNameIndex in MsFormsConfig");

                    var student = students.SingleOrDefault(s => s.FullName == fullName) ?? students.SingleOrDefault(s => s.FullName.Contains(fullName));
                    if (student == null)
                    {
                        student = new Student(fullName, "", "", students.First().LevelOfStudy, students.First().ProgrammeCode, students.First().CurriculumCode, "???");
                        students.Add(student);
                    }

                    var questionNumber = 1;
                    var scores = new Dictionary<Question, int>();
                    for (var j = config.StartQuestionIndex; j < columnsCount; j += config.Shift)
                    {
                        var score = int.Parse(cellsWithAnswers[j + 1].CellValue.InnerText);
                        var question = questions.Single(q => q.Number == questionNumber);
                        scores.Add(question, score);
                        ++questionNumber;
                    }
                    studentAnswers.Add(new StudentAnswer(student, discipline, scores));
                }
                catch (Exception e) when (e is FormatException || e is IndexOutOfRangeException || e is InvalidOperationException || e is ArgumentNullException)
                {
                    studentAnswers.Clear();
                    return false;
                }
                catch (Exception e)
                {
                    studentAnswers.Clear();
                    return false;
                }
            }
            return true;
        }
    }
}
